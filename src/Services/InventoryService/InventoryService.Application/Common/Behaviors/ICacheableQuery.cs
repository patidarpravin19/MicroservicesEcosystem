namespace InventoryService.Application.Common.Behaviors;

/// <summary>
/// Implemented by any MediatR query that should be transparently served from Redis.
/// CacheKey should encode every parameter that affects the result (e.g. the SKU).
/// CacheExpiry controls how long a cache miss result stays valid before re-fetch.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }
    TimeSpan CacheExpiry { get; }
}
