using System.Security.Claims;
using AccountingInventory.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;

namespace AccountingInventory.Infrastructure.Persistence;

public sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
