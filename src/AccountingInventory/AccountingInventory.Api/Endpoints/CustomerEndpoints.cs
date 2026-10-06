using AccountingInventory.Application.Customers;
using AccountingInventory.Application.Customers.Queries.SearchCustomers;
using BuildingBlocks.WebDefaults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingInventory.Api.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .RequireAuthorization("AuthenticatedUser")
            .AddEndpointFilter<TenantHeaderEndpointFilter>()
            .WithMetadata(new RequiresTenantIdHeaderAttribute());

        group.MapGet("/", async ([FromQuery] string? search, [FromQuery] int? limit,
                ISender sender, CancellationToken cancellationToken) =>
                Results.Ok(await sender.Send(new SearchCustomersQuery(search, limit ?? 10), cancellationToken)))
            .WithName("SearchCustomers")
            .Produces<IReadOnlyList<CustomerLookupSummary>>(StatusCodes.Status200OK);

        return group;
    }
}
