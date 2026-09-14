using BuildingBlocks.Security;
using InventoryService.Application.StockItems.Commands.AddStock;
using InventoryService.Application.StockItems.Commands.CreateStockItem;
using InventoryService.Application.StockItems.Queries.GetStock;
using MediatR;

namespace InventoryService.Api.Endpoints;

public static class StockEndpoints
{
    public static RouteGroupBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stock-items").WithTags("StockItems");

        group.MapPost("/", async (CreateStockItemCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/stock-items/{result.Sku}", result);
            })
            .WithName("CreateStockItem")
            .RequirePermission("Inventory.StockItems.Create")
            .Produces<CreateStockItemResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{sku}/add-stock", async (string sku, AddStockRequestBody body, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new AddStockCommand(sku, body.Quantity), ct);
                return Results.Ok(result);
            })
            .WithName("AddStock")
            .RequirePermission("Inventory.StockItems.AddStock")
            .Produces<AddStockResult>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{sku}", async (string sku, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetStockQuery(sku), ct)))
            .WithName("GetStock")
            .RequirePermission("Inventory.StockItems.Read")
            .Produces<GetStockResult>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}

public sealed record AddStockRequestBody(int Quantity);
