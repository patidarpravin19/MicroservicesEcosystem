namespace InventoryService.Domain.Common;

/// <summary>
/// Base type for every aggregate/entity in this service that must be automatically
/// audited by the infrastructure-layer SaveChanges interceptor. Copy this file
/// unchanged into any new "XxxService.Domain" project cloned from this Gold Master.
/// </summary>
public abstract class AuditableEntity
{
    public required Guid Id { get; init; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
}
