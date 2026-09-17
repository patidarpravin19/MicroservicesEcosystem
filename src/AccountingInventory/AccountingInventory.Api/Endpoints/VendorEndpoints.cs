using AccountingInventory.Application.Tenants.Queries.GetTenantBySlug;
using AccountingInventory.Application.Tenants.Queries.GetTenants;
using AccountingInventory.Application.Vendors.Commands.AddVendor;
using MediatR;

namespace AccountingInventory.Api.Endpoints;

public static class VendorEndpoints
{
    public static RouteGroupBuilder MapVendorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vendors")
            .WithTags("Vendors")
            .RequireAuthorization("AuthenticatedUser")
            // Tenant-specific vendor data always requires X-Tenant-Id. The filter
            // resolves its schema from the tenant registry before MediatR creates a
            // tenant-scoped DbContext.
            .AddEndpointFilter<TenantHeaderEndpointFilter>();

        // The tenant is selected by X-Tenant-Id and resolved server-side by the
        // group filter. The header must match the authenticated user's tenant.
        group.MapPost("/add", async (AddVendorCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/vendors", result);
            })
            .WithName("AddVendor")
            .Produces<AddVendorResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/by-slug/{slug}", async (string slug, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetTenantBySlugQuery(slug), ct)))
            .WithName("GetVendorBySlug")
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
