using AccountingInventory.Application.Vendors.Commands.AddVendor;
using AccountingInventory.Application.Vendors.Commands.UpdateVendor;
using AccountingInventory.Application.Vendors.Queries.GetVendorById;
using AccountingInventory.Application.Vendors.Queries.GetVendors;
using BuildingBlocks.WebDefaults;
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
            .AddEndpointFilter<TenantHeaderEndpointFilter>()
            .WithMetadata(new RequiresTenantIdHeaderAttribute());

        // The tenant is selected by X-Tenant-Id and resolved server-side by the
        // group filter. The header must match the authenticated user's tenant.
        group.MapPost("/", async (AddVendorCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/vendors", result);
            })
            .WithName("AddVendor")
            .Produces<AddVendorResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id}", async (Guid id, ISender sender, CancellationToken ct) =>
                 Results.Ok(await sender.Send(new GetVendorByIdQuery(id), ct)))
            .WithName("GetVendorById")
            .Produces<Application.Vendors.Queries.GetVendorById.VendorSummary>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (ISender sender, CancellationToken ct) => 
                Results.Ok(await sender.Send(new GetVendorsQuery(), ct)))
            .WithName("GetVendors");     

        group.MapPut("/{id:guid}", async (Guid id, UpdateVendorCommand command, ISender sender, CancellationToken ct) =>
        {           
            if (id != command.Id)
            {
                return Results.BadRequest("ID in route does not match ID in body.");
            }

            var result = await sender.Send(command, ct);

            // 2. Return 200 OK or 204 No Content for a successful update
            return Results.Ok(result);
        })
            .WithName("UpdateVendor")
            .Produces<UpdateVendorResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);
    

        //group.MapPost("/{id:guid}/reactivate", async (Guid id, ISender sender, CancellationToken ct) =>
        //        Results.Ok(await sender.Send(new ReactivateTenantCommand(id), ct)))
        //    .WithName("ReactivateVendor");

        return group;
    }
}
