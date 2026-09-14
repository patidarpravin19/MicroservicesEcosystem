namespace BuildingBlocks.Domain;

/// <summary>
/// Base type for every aggregate/entity in ANY service that must be automatically
/// audited by the shared BuildingBlocks.Persistence SaveChanges interceptor. Living
/// here (rather than copy-pasted per service, as in the original Gold Master) means
/// the interceptor can be written once and reused by every service's DbContext,
/// which matters once services can be hosted together in a single process.
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
