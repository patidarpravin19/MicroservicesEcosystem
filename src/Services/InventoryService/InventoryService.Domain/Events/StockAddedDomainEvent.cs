using BuildingBlocks.Domain;

namespace InventoryService.Domain.Events;

public sealed record StockAddedDomainEvent(
    Guid StockItemId,
    string Sku,
    int QuantityAdded,
    int NewQuantityOnHand,
    DateTimeOffset OccurredOnUtc) : IDomainEvent;
