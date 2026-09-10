using InventoryService.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior implementing the read-through cache pattern: any request
/// implementing ICacheableQuery is checked against Redis first; on a miss the inner
/// handler runs against PostgreSQL and the result is written back to Redis before being
/// returned. Non-cacheable requests (including all commands) pass straight through.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(
    ICacheService cacheService,
    ILogger<CachingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheableQuery)
        {
            return await next(cancellationToken);
        }

        var cached = await cacheService.GetAsync<TResponse>(cacheableQuery.CacheKey, cancellationToken);

        if (cached is not null)
        {
            logger.LogInformation("Cache HIT for key {CacheKey}", cacheableQuery.CacheKey);
            return cached;
        }

        logger.LogInformation("Cache MISS for key {CacheKey}", cacheableQuery.CacheKey);

        var response = await next(cancellationToken);

        if (response is not null)
        {
            await cacheService.SetAsync(cacheableQuery.CacheKey, response, cacheableQuery.CacheExpiry, cancellationToken);
        }

        return response;
    }
}
