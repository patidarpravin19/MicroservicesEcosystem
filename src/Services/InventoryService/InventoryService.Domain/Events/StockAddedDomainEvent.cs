using InventoryService.Domain.Common;

namespace InventoryService.Domain.Events;

/// <summary>
/// Raised in-process by the StockItem aggregate whenever quantity is added.
/// InventoryService.Infrastructure listens for this after a successful SaveChanges
/// and translates it into the StockAddedIntegrationEvent published via MassTransit.
/// </summary>
public sealed record StockAddedDomainEvent(
    Guid StockItemId,
    string Sku,
    int QuantityAdded,
    int NewQuantityOnHand,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
