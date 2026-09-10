using InventoryService.Application.Abstractions;
using InventoryService.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.StockItems.Queries.GetStock;

public sealed class GetStockQueryHandler(IInventoryDbContext db, ILogger<GetStockQueryHandler> logger)
    : IRequestHandler<GetStockQuery, GetStockResult>
{
    public async Task<GetStockResult> Handle(GetStockQuery request, CancellationToken cancellationToken)
    {
        var normalizedSku = request.Sku.ToUpperInvariant();

        var stockItem = await db.StockItems
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Sku == normalizedSku, cancellationToken);

        if (stockItem is null)
        {
            logger.LogWarning("Stock lookup failed: no stock item exists for SKU {Sku}.", normalizedSku);
            throw new NotFoundException($"No stock item exists for SKU '{normalizedSku}'.");
        }

        return new GetStockResult(
            stockItem.Id, stockItem.Sku, stockItem.DisplayName, stockItem.QuantityOnHand, stockItem.WarehouseLocation);
    }
}
