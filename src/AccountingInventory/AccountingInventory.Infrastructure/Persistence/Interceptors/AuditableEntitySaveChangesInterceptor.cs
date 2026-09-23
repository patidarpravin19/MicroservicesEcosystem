using BuildingBlocks.Domain;
using BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AccountingInventory.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core SaveChanges interceptor that stamps CreatedAt/CreatedBy on insert and
/// ModifiedAt/ModifiedBy on update for every AuditableEntity — automatically,
/// with zero code required in Application-layer handlers. Deletes are converted to
/// soft-deletes so audit history is never lost.
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

        var actor = currentUserProvider.UserId;
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.CreatedBy).CurrentValue = actor;
                    break;

                case EntityState.Modified:
                    entry.Property(e => e.ModifiedAt).CurrentValue = now;
                    entry.Property(e => e.ModifiedBy).CurrentValue = actor;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Property(e => e.IsDeleted).CurrentValue = true;
                    entry.Property(e => e.ModifiedAt).CurrentValue = now;
                    entry.Property(e => e.ModifiedBy).CurrentValue = actor;
                    break;
            }
        }
    }
}
