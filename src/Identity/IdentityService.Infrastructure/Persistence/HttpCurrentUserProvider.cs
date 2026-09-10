using System.Security.Claims;
using IdentityService.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Http;

namespace IdentityService.Infrastructure.Persistence;

public sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
