using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using AccountingInventory.Infrastructure.Persistence.Configurations;
using AccountingInventory.Infrastructure.Persistence.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Infrastructure.Persistence;

/// <summary>
/// Schema-per-tenant: Users and Vendors. The active schema is supplied by
/// <see cref="ITenantProvider"/> and is part of the EF model cache key.
///
/// Configurations are applied explicitly (NOT via ApplyConfigurationsFromAssembly)
/// because this project's assembly also contains TenantConfiguration, which belongs
/// exclusively to TenantDbContext (a separate database entirely — see
/// DependencyInjection.AddControlPlaneDbContext) — an assembly-wide scan here would
/// incorrectly pull that unrelated table into this context's migrations too.
/// </summary>
public sealed class AccountingInventoryDbContext(
    DbContextOptions<AccountingInventoryDbContext> options,
    ITenantProvider tenantProvider)
    : DbContext(options), IAccountingInventoryDbContext
{
    public string SchemaName { get; } = tenantProvider.SchemaName;

    public DbSet<User> Users => Set<User>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<ProductModel> ProductModels => Set<ProductModel>();

    //public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema selection is deliberately connection-scoped (Npgsql Search Path),
        // not model-scoped.  EF migrations must contain unqualified table names so
        // the same migration can run in every tenant schema.
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new VendorConfiguration());
        modelBuilder.ApplyConfiguration(new BrandConfiguration());
        modelBuilder.ApplyConfiguration(new ProductTypeConfiguration());
        //modelBuilder.ApplyConfiguration(new RoleConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public Task ResetConnectionAsync(CancellationToken cancellationToken)
        => Database.CloseConnectionAsync();
}
