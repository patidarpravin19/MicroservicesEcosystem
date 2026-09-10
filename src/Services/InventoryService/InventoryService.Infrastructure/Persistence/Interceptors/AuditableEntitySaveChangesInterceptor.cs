using InventoryService.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InventoryService.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that stamps CreatedAtUtc/CreatedBy on insert and
/// LastModifiedAtUtc/LastModifiedBy on update for every AuditableEntity — automatically,
/// with zero code required in Application-layer handlers. Deletes are converted to
/// soft-deletes so audit history is never lost.
///
/// This exact file can be copied unmodified into any new "XxxService.Infrastructure"
/// project cloned from this Gold Master.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor(ICurrentUserProvider currentUserProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditInfo(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditInfo(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditInfo(DbContext? context)
    {
        if (context is null) return;

        var actor = currentUserProvider.UserId ?? "system";
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAtUtc).CurrentValue = now;
                    entry.Property(e => e.CreatedBy).CurrentValue = actor;
                    break;

                case EntityState.Modified:
                    entry.Property(e => e.LastModifiedAtUtc).CurrentValue = now;
                    entry.Property(e => e.LastModifiedBy).CurrentValue = actor;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Property(e => e.IsDeleted).CurrentValue = true;
                    entry.Property(e => e.LastModifiedAtUtc).CurrentValue = now;
                    entry.Property(e => e.LastModifiedBy).CurrentValue = actor;
                    break;
            }
        }
    }
}

public interface ICurrentUserProvider
{
    string? UserId { get; }
}
