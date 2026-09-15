using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using AccountingInventory.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Infrastructure.Persistence;

/// <summary>
/// The shared tenant registry — lives in the "tenant" schema of the SAME
/// "EcosystemDb" PostgreSQL database that every tenant's own schema
/// (IdentityDbContext) also lives in. One database, multiple schemas:
///   - "tenant"                     → this context: the registry itself (shared/master data)
///   - "tenant_acme_3f2a1b4c", ...  → IdentityDbContext, one schema per tenant
/// Migrated once at application startup, independent of any tenant's schema
/// migrations (see TenantSchemaProvisioner, used from RegisterTenantCommandHandler) —
/// its own, separately-named migrations history table guarantees that even though
/// both contexts share a database, their migration sets can never collide.
/// </summary>
public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options)
    : DbContext(options), ITenantDirectoryContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}