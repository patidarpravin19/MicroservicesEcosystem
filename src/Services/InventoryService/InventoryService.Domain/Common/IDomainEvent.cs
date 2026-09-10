namespace InventoryService.Domain.Common;

/// <summary>
/// Marker for internal domain events raised by aggregates during a business
/// operation. These are distinct from the integration events published to
/// RabbitMQ via MassTransit (see InventoryService.Application/Contracts) — a
/// domain event stays in-process until the Infrastructure layer decides to
/// translate it into an outbound integration event after SaveChanges succeeds.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>
/// Base type for any aggregate root that raises domain events. Copy unchanged
/// into new Gold-Master-derived services.
/// </summary>
public abstract class AggregateRoot : AuditableEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
