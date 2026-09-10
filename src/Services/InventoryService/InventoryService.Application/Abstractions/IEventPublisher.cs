namespace InventoryService.Application.Abstractions;

/// <summary>
/// Dependency-inversion seam over the message broker. Implemented in Infrastructure
/// (wrapping MassTransit's IPublishEndpoint) so Application handlers depend only on
/// this narrow contract rather than the messaging library's full surface area.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class;
}
