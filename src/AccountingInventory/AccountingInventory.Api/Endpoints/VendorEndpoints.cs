using BuildingBlocks.Security;
using AccountingInventory.Application.Tenants.Commands.ReactivateTenant;
using AccountingInventory.Application.Tenants.Commands.RegisterTenant;
using AccountingInventory.Application.Tenants.Commands.SuspendTenant;
using AccountingInventory.Application.Tenants.Queries.GetTenantBySlug;
using AccountingInventory.Application.Tenants.Queries.GetTenants;
using MediatR;
using AccountingInventory.Application.Vendors.Commands.AddVendor;

namespace AccountingInventory.Api.Endpoints;

public static class VendorEndpoints
{
    public static RouteGroupBuilder MapVendorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vendors").WithTags("Vendors");

        // Deliberately anonymous — this is vendor self-service signup. In production,
        // put rate-limiting and/or CAPTCHA in front of it (the Gateway's rate limiter
        // already applies at the edge) and consider adding email verification before
        // a vendor is marked Active.
        group.MapPost("/add", async (AddVendorCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/vendors", result);
            })
            .WithName("AddVendor").AllowAnonymous()
            .Produces<AddVendorResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/by-slug/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTenantBySlugQuery(slug), ct)))
            .WithName("GetVendorBySlug").AllowAnonymous()
            .Produces<TenantSummary>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTenantsQuery(), ct)))
            .WithName("GetVendors");

        //group.MapPost("/{id:guid}/suspend", async (Guid id, ISender sender, CancellationToken ct) =>
        //        Results.Ok(await sender.Send(new SuspendTenantCommand(id), ct)))
        //    .WithName("SuspendVendor");

        //group.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
        //        Results.Ok(await sender.Send(new ReactivateTenantCommand(id), ct)))
        //    .WithName("ReactivateVendor");

        return group;
    }
}
