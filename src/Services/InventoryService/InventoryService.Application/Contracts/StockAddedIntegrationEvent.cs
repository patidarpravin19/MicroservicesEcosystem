namespace InventoryService.Application.Contracts;

/// <summary>
/// Message contract published to RabbitMQ via MassTransit whenever stock is added.
/// This is the wire-format record any consumer (in this service or another) subscribes
/// to — deliberately decoupled from the internal domain event shape.
/// </summary>
public sealed record StockAddedIntegrationEvent(
    Guid StockItemId,
    string Sku,
    int QuantityAdded,
    int NewQuantityOnHand,
    DateTimeOffset OccurredOnUtc,
    string CorrelationId);
