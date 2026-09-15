using BuildingBlocks.Security;
using AccountingInventory.Application.Tenants.Commands.ReactivateTenant;
using AccountingInventory.Application.Tenants.Commands.RegisterTenant;
using AccountingInventory.Application.Tenants.Commands.SuspendTenant;
using AccountingInventory.Application.Tenants.Queries.GetTenantBySlug;
using AccountingInventory.Application.Tenants.Queries.GetTenants;
using MediatR;

namespace AccountingInventory.Api.Endpoints;

public static class TenantEndpoints
{
    public static RouteGroupBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tenants").WithTags("Tenants");

        // Deliberately anonymous — this is tenant self-service signup. In production,
        // put rate-limiting and/or CAPTCHA in front of it (the Gateway's rate limiter
        // already applies at the edge) and consider adding email verification before
        // a tenant is marked Active.
        group.MapPost("/register", async (RegisterTenantCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/tenants/{result.TenantId}", result);
            })
            .WithName("RegisterTenant").AllowAnonymous()
            .Produces<RegisterTenantResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/by-slug/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTenantBySlugQuery(slug), ct)))
            .WithName("GetTenantBySlug").AllowAnonymous()
            .Produces<TenantSummary>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTenantsQuery(), ct)))
            .WithName("GetTenants").RequirePermission("Tenants.Manage");

        group.MapPost("/{id:guid}/suspend", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new SuspendTenantCommand(id), ct)))
            .WithName("SuspendTenant").RequirePermission("Tenants.Manage");

        group.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ReactivateTenantCommand(id), ct)))
            .WithName("ReactivateTenant").RequirePermission("Tenants.Manage");

        return group;
    }
}
