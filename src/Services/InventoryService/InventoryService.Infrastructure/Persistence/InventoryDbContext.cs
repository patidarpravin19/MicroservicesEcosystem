using System.Reflection;
using InventoryService.Application.Abstractions;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Persistence;

public sealed class InventoryDbContext(
    DbContextOptions<InventoryDbContext> options,
    AuditableEntitySaveChangesInterceptor auditInterceptor)
    : DbContext(options), IInventoryDbContext
{
    public DbSet<StockItem> StockItems => Set<StockItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.AddInterceptors(auditInterceptor);
}
