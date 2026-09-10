using InventoryService.Application.Abstractions;
using InventoryService.Application.Common.Exceptions;
using InventoryService.Application.Contracts;
using InventoryService.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.StockItems.Commands.AddStock;

/// <summary>
/// Write path of the Gold Master vertical slice.
///
/// Flow: validated by FluentValidation via ValidationBehavior (see the pipeline
/// registration in DependencyInjection) → mutates the StockItem aggregate in-memory →
/// persists via IInventoryDbContext/PostgreSQL → publishes StockAddedIntegrationEvent
/// via MassTransit/RabbitMQ → invalidates the corresponding Redis cache key for
/// GetStockQuery so subsequent reads are never stale.
/// </summary>
public sealed class AddStockCommandHandler(
    IInventoryDbContext db,
    IEventPublisher eventPublisher,
    ICacheService cacheService,
    ILogger<AddStockCommandHandler> logger)
    : IRequestHandler<AddStockCommand, AddStockResult>
{
    public async Task<AddStockResult> Handle(AddStockCommand request, CancellationToken cancellationToken)
    {
        var normalizedSku = request.Sku.ToUpperInvariant();

        var stockItem = await db.StockItems.SingleOrDefaultAsync(s => s.Sku == normalizedSku, cancellationToken)
            ?? throw new NotFoundException($"No stock item exists for SKU '{normalizedSku}'.");

        stockItem.AddStock(request.Quantity);

        await db.SaveChangesAsync(cancellationToken);

        var domainEvent = stockItem.DomainEvents.OfType<StockAddedDomainEvent>().Last();
        stockItem.ClearDomainEvents();

        await eventPublisher.PublishAsync(
            new StockAddedIntegrationEvent(
                domainEvent.StockItemId,
                domainEvent.Sku,
                domainEvent.QuantityAdded,
                domainEvent.NewQuantityOnHand,
                domainEvent.OccurredOnUtc,
                CorrelationId: Guid.NewGuid().ToString("N")),
            cancellationToken);

        var cacheKey = Queries.GetStock.GetStockQuery.BuildCacheKey(normalizedSku);
        await cacheService.RemoveAsync(cacheKey, cancellationToken);

        logger.LogInformation(
            "Stock added for {Sku}: +{Quantity} -> {NewQuantity}. Cache key {CacheKey} invalidated.",
            normalizedSku, request.Quantity, stockItem.QuantityOnHand, cacheKey);

        return new AddStockResult(stockItem.Id, stockItem.Sku, stockItem.QuantityOnHand);
    }
}
