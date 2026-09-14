using System.Reflection;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

/// <summary>
/// Schema-per-tenant: StockItems. No HasDefaultSchema call — which physical schema
/// this context's queries land in is decided by TenantSchemaConnectionInterceptor
/// (BuildingBlocks.Persistence) at connection-open time, from the caller's JWT
/// tenant_schema claim.
/// </summary>
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options)
    : DbContext(options), IInventoryDbContext
{
    public DbSet<StockItem> StockItems => Set<StockItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
