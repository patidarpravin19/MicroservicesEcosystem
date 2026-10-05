using AccountingInventory.Application.Common.Models;
using AccountingInventory.Application.Inventory.Queries.GetStock;
using BuildingBlocks.WebDefaults;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountingInventory.Api.Endpoints;

public static class InventoryEndpoints
{
    public static RouteGroupBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory")
            .WithTags("Inventory")
            .RequireAuthorization("AuthenticatedUser")
            .AddEndpointFilter<TenantHeaderEndpointFilter>()
            .WithMetadata(new RequiresTenantIdHeaderAttribute());

        group.MapGet("/stock", async ([FromQuery] int? page, [FromQuery] int? pageSize,
            [FromQuery] string? search, ISender sender, CancellationToken cancellationToken) =>
            Results.Ok(await sender.Send(new GetStockQuery(page ?? 1, pageSize ?? 20, search), cancellationToken)))
            .WithName("GetStockInventory")
            .Produces<PagedResult<StockItemSummary>>();

        return group;
    }
}
