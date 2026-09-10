using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Abstractions;

/// <summary>
/// Dependency-inversion seam: the Application layer only knows about this interface,
/// never about EF Core's DbContext or Npgsql. IdentityService.Infrastructure provides
/// the concrete implementation (IdentityDbContext).
/// </summary>
public interface IIdentityDbContext
{
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
