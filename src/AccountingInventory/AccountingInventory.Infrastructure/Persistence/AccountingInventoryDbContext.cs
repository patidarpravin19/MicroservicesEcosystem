using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using AccountingInventory.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Infrastructure.Persistence;

/// <summary>
/// Schema-per-tenant: Users and Roles. Which physical PostgreSQL schema this
/// context's queries land in is decided entirely by TenantSchemaConnectionInterceptor
/// at connection-open time (see BuildingBlocks.Persistence), never by anything in
/// this class — deliberately no HasDefaultSchema call here.
///
/// Configurations are applied explicitly (NOT via ApplyConfigurationsFromAssembly)
/// because this project's assembly also contains TenantConfiguration, which belongs
/// exclusively to TenantDbContext (a separate database entirely — see
/// DependencyInjection.AddControlPlaneDbContext) — an assembly-wide scan here would
/// incorrectly pull that unrelated table into this context's migrations too.
/// </summary>
public sealed class AccountingInventoryDbContext(DbContextOptions<AccountingInventoryDbContext> options)
    : DbContext(options), IAccountingInventoryDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
        base.OnModelCreating(modelBuilder);
    }

    public Task ResetConnectionAsync(CancellationToken cancellationToken)
        => Database.CloseConnectionAsync();
}
