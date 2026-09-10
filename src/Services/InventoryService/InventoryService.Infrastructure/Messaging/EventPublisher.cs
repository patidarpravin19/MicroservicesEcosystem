using InventoryService.Application.Abstractions;
using MassTransit;

namespace InventoryService.Infrastructure.Messaging;

/// <summary>
/// Concrete implementation of the Application layer's IEventPublisher abstraction,
/// wrapping MassTransit's IPublishEndpoint. This is the only place in the whole
/// solution where Application-facing code touches MassTransit directly, keeping the
/// dependency direction correct (Infrastructure depends on the messaging library, not
/// the other way around).
/// </summary>
public sealed class EventPublisher(IPublishEndpoint publishEndpoint) : IEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class
        => publishEndpoint.Publish(integrationEvent, cancellationToken);
}
