using InventoryService.Application.StockItems.Commands.AddStock;
using InventoryService.Application.StockItems.Commands.CreateStockItem;
using InventoryService.Application.StockItems.Queries.GetStock;
using MediatR;

namespace InventoryService.Api.Endpoints;

/// <summary>
/// Maps the Gold Master vertical slice: creating a stock item, adding stock (write
/// path — validation, persistence, event publish, cache invalidation), and reading
/// stock (read path — transparent Redis caching via CachingBehavior).
/// </summary>
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
            .RequireAuthorization("AuthenticatedUser")
            .Produces<CreateStockItemResult>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{sku}/add-stock", async (string sku, AddStockRequestBody body, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new AddStockCommand(sku, body.Quantity), ct);
                return Results.Ok(result);
            })
            .WithName("AddStock")
            .RequireAuthorization("AuthenticatedUser")
            .Produces<AddStockResult>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{sku}", async (string sku, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetStockQuery(sku), ct)))
            .WithName("GetStock")
            .RequireAuthorization("AuthenticatedUser")
            .Produces<GetStockResult>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }
}

public sealed record AddStockRequestBody(int Quantity);
