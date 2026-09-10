namespace InventoryService.Application.Abstractions;

/// <summary>
/// Dependency-inversion seam over the distributed cache. Implemented in Infrastructure
/// via StackExchange.Redis so the Application layer's CachingBehavior never depends on
/// a specific caching technology.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken);

    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken);

    Task RemoveAsync(string key, CancellationToken cancellationToken);

    Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken);
}
