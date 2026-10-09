using BuildingBlocks.Security;
using AccountingInventory.Application.Tenants.Commands.ApproveTenant;
using AccountingInventory.Application.Tenants.Commands.ReactivateTenant;
using AccountingInventory.Application.Tenants.Commands.RegisterTenant;
using AccountingInventory.Application.Tenants.Commands.RejectTenant;
using AccountingInventory.Application.Tenants.Commands.SuspendTenant;
using AccountingInventory.Application.Tenants.Queries.GetPendingTenants;
using AccountingInventory.Application.Tenants.Queries.GetTenantBySlug;
using AccountingInventory.Application.Tenants.Queries.GetTenants;
using MediatR;

namespace AccountingInventory.Api.Endpoints;

public static class TenantEndpoints
{
    public static RouteGroupBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Tenants");

        // Self-service tenant signup: registers store and sends confirmation + approval emails
        group.MapPost("/register", async (RegisterTenantCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/tenants/{result.TenantId}", result);
            })
            .WithName("RegisterTenant").AllowAnonymous()
            .Produces<RegisterTenantResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        // List stores waiting for administrator approval (Supports initial offline cold-start bootstrapping)
        group.MapGet("/pending", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetPendingTenantsQuery(), ct)))
            .WithName("GetPendingTenants").AllowAnonymous()
            .Produces<IReadOnlyList<PendingTenantDto>>();

        // Administrator approval: triggers automatic schema creation, EF migrations & seed data
        group.MapPost("/{id:guid}/approve", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ApproveTenantCommand(id), ct)))
            .WithName("ApproveTenant").AllowAnonymous()
            .Produces<ApproveTenantResult>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Administrator rejection with reason
        group.MapPost("/{id:guid}/reject", async (Guid id, RejectTenantRequest request, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new RejectTenantCommand(id, request?.Reason), ct)))
            .WithName("RejectTenant").AllowAnonymous()
            .Produces<RejectTenantResult>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/by-slug/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTenantBySlugQuery(slug), ct)))
            .WithName("GetTenantBySlug").AllowAnonymous()
            .Produces<TenantSummary>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTenantsQuery(), ct)))
            .WithName("GetTenants").RequireAuthorization().RequirePermission("Tenants.Manage");

        group.MapPost("/{id:guid}/suspend", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new SuspendTenantCommand(id), ct)))
            .WithName("SuspendTenant").RequireAuthorization().RequirePermission("Tenants.Manage");

        group.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ReactivateTenantCommand(id), ct)))
            .WithName("ReactivateTenant").RequireAuthorization().RequirePermission("Tenants.Manage");

        return group;
    }
}

public sealed record RejectTenantRequest(string? Reason = null);
