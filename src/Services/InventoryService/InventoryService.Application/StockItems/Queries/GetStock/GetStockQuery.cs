using InventoryService.Application.Common.Behaviors;
using MediatR;

namespace InventoryService.Application.StockItems.Queries.GetStock;

/// <summary>
/// Read path of the Gold Master vertical slice. Implements ICacheableQuery so
/// CachingBehavior transparently serves this from Redis when available, falling back
/// to PostgreSQL on a cache miss and repopulating Redis with the result.
/// </summary>
public sealed record GetStockQuery(string Sku) : IRequest<GetStockResult>, ICacheableQuery
{
    public string CacheKey => BuildCacheKey(Sku.ToUpperInvariant());
    public TimeSpan CacheExpiry => TimeSpan.FromMinutes(10);

    public static string BuildCacheKey(string normalizedSku) => $"inventory:stock:{normalizedSku}";
}

public sealed record GetStockResult(Guid StockItemId, string Sku, string DisplayName, int QuantityOnHand, string WarehouseLocation);
