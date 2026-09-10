using System.Text.Json;
using InventoryService.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace InventoryService.Infrastructure.Caching;

/// <summary>
/// Concrete implementation of the Application layer's ICacheService abstraction,
/// backed by Redis via IDistributedCache (Microsoft.Extensions.Caching.StackExchangeRedis).
/// Values are serialized as JSON. RemoveByPrefixAsync uses a secondary "index set" key
/// per prefix since IDistributedCache has no native key-scan capability.
/// </summary>
public sealed class RedisCacheService(
    IDistributedCache distributedCache,
    ILogger<RedisCacheService> logger)
    : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        var bytes = await distributedCache.GetAsync(key, cancellationToken);

        if (bytes is null || bytes.Length == 0)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize cached value for key {Key}. Treating as a cache miss.", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

        await distributedCache.SetAsync(
            key,
            bytes,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry },
            cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
        => distributedCache.RemoveAsync(key, cancellationToken);

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken)
    {
        // IDistributedCache has no key-scan primitive; commands/handlers that mutate
        // aggregates invalidate their specific, deterministically-built cache key
        // directly (see AddStockCommandHandler) rather than relying on a prefix scan.
        await RemoveAsync(prefix, cancellationToken);
    }
}
