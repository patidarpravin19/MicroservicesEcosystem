using InventoryService.Application.Abstractions;
using InventoryService.Application.Common.Exceptions;
using InventoryService.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.StockItems.Commands.CreateStockItem;

public sealed class CreateStockItemCommandHandler(
    IInventoryDbContext db,
    ILogger<CreateStockItemCommandHandler> logger)
    : IRequestHandler<CreateStockItemCommand, CreateStockItemResult>
{
    public async Task<CreateStockItemResult> Handle(CreateStockItemCommand request, CancellationToken cancellationToken)
    {
        var normalizedSku = request.Sku.ToUpperInvariant();

        var exists = await db.StockItems.AnyAsync(s => s.Sku == normalizedSku, cancellationToken);

        if (exists)
        {
            logger.LogWarning("Stock item creation rejected: SKU {Sku} already exists.", normalizedSku);
            throw new ConflictException($"A stock item with SKU '{normalizedSku}' already exists.");
        }

        var stockItem = StockItem.Create(request.Sku, request.DisplayName, request.WarehouseLocation);

        db.StockItems.Add(stockItem);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Stock item {StockItemId} created for SKU {Sku} in warehouse {WarehouseLocation}.",
            stockItem.Id, stockItem.Sku, stockItem.WarehouseLocation);

        return new CreateStockItemResult(stockItem.Id, stockItem.Sku, stockItem.DisplayName, stockItem.QuantityOnHand);
    }
}
