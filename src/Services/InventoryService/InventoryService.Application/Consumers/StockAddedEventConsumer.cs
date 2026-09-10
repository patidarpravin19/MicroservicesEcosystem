using InventoryService.Application.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.Consumers;

/// <summary>
/// Sample MassTransit consumer showing how incoming integration events are safely
/// processed within the Application layer. In this Gold Master it demonstrates the
/// pattern by reacting to the service's own StockAddedIntegrationEvent (e.g. to
/// drive a low-stock alert or an audit trail) — a real "OrderService" clone would
/// instead consume events published by other bounded contexts (e.g. OrderPlacedEvent)
/// using this exact shape.
///
/// MassTransit invokes Consume inside its own retry/DLQ pipeline (configured in
/// InventoryService.Infrastructure), so unhandled exceptions here are automatically
/// retried with backoff and eventually dead-lettered — no manual retry code needed.
/// </summary>
public sealed class StockAddedEventConsumer(ILogger<StockAddedEventConsumer> logger)
    : IConsumer<StockAddedIntegrationEvent>
{
    private const int LowStockThreshold = 10;

    public Task Consume(ConsumeContext<StockAddedIntegrationEvent> context)
    {
        var message = context.Message;

        logger.LogInformation(
            "Processed StockAddedIntegrationEvent for {Sku}: +{QuantityAdded} -> {NewQuantityOnHand} " +
            "(CorrelationId={CorrelationId})",
            message.Sku, message.QuantityAdded, message.NewQuantityOnHand, message.CorrelationId);

        if (message.NewQuantityOnHand <= LowStockThreshold)
        {
            logger.LogWarning(
                "Low stock alert: {Sku} has only {NewQuantityOnHand} units remaining.",
                message.Sku, message.NewQuantityOnHand);
        }

        return Task.CompletedTask;
    }
}
