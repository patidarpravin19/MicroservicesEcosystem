using System.Security.Claims;
using InventoryService.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;

namespace InventoryService.Infrastructure.Persistence;

public sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
