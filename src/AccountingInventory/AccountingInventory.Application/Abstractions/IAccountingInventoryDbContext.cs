using AccountingInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Abstractions;

/// <summary>
/// Schema-per-tenant data (backed by the "EcosystemDb" database): Users and Roles for
/// the CURRENT tenant, whichever schema that resolves to via ITenantContext.
/// </summary>
public interface IAccountingInventoryDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Forces the underlying connection closed so the next query opens a fresh one.
    /// Required immediately after resolving a tenant's schema mid-request (Register,
    /// Login) and calling ITenantContextAccessor.SetTenant — otherwise the connection
    /// already open from an earlier query keeps the OLD search_path. See
    /// TenantSchemaConnectionInterceptor.
    /// </summary>
    Task ResetConnectionAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Shared, non-tenant-scoped data: the tenant registry itself (backed by its own,
/// separate "TenantDb" database — not a schema inside "EcosystemDb"). This is where a
/// tenant's Name/Slug/SchemaName/Status live, reachable before any tenant schema is
/// known (Login/Register both resolve a slug through here first) and reachable no
/// matter how many per-tenant schemas exist in the other database.
/// </summary>
public interface ITenantDirectoryContext
{
    DbSet<Tenant> Tenants { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
