using System.Data;
using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.GeneralLedger;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AccountingInventory.Infrastructure.Persistence;

// Balance/stock/period checks and all writes must observe one transaction.
// Serializable isolation rejects concurrent writes that would invalidate a check.
public sealed class BusinessTransactionBehavior<TRequest, TResponse>(AccountingInventoryDbContext db, IRequestIdentity identity)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!typeof(TRequest).Name.EndsWith("Command", StringComparison.Ordinal)
            || typeof(TRequest).Namespace?.Contains(".Tenants.", StringComparison.Ordinal) == true
            || (typeof(TRequest).Namespace?.Contains(".Users.", StringComparison.Ordinal) == true
                && typeof(TRequest).Name != "RegisterCommand")
            || db.Database.CurrentTransaction is not null)
            return await next();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var requestNamespace = typeof(TRequest).Namespace ?? "";
            var permission = request is IBusinessPermissionRequest permissionRequest ? permissionRequest.BusinessPermission
                : request is RefundCorrectionCommand ? null
                : requestNamespace.Contains(".Purchases.", StringComparison.Ordinal) ? "purchases.manage"
                : requestNamespace.Contains(".Sales.", StringComparison.Ordinal) ? "sales.manage"
                : requestNamespace.Contains(".Inventory", StringComparison.Ordinal) ? "inventory.manage"
                : requestNamespace.Contains(".GeneralLedger", StringComparison.Ordinal) ? "accounting.manage"
                : requestNamespace.Contains(".Users.", StringComparison.Ordinal) ? null
                : "catalog.manage";
            if (!string.IsNullOrEmpty(permission))
                await AccountingPermissionGate.EnsureAsync(db, identity.UserId, permission, ct);
            var response = await next();
            await transaction.CommitAsync(ct);
            return response;
        }
        catch (Exception exception) when (FindPostgres(exception) is { } postgres
            && postgres.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
                or PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw new ConflictException("The record or balance changed while this request was processed. Refresh and retry; no changes were saved.");
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }

    private static PostgresException? FindPostgres(Exception? exception)
    {
        while (exception is not null)
        {
            if (exception is PostgresException postgres) return postgres;
            exception = exception.InnerException;
        }
        return null;
    }
}
