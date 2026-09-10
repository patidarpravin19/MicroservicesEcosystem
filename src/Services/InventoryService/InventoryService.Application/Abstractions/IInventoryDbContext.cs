using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Application.Abstractions;

/// <summary>
/// Dependency-inversion seam: the Application layer only knows about this interface,
/// never about EF Core's DbContext or Npgsql directly. InventoryService.Infrastructure
/// provides the concrete implementation (InventoryDbContext).
/// </summary>
public interface IInventoryDbContext
{
    DbSet<StockItem> StockItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
