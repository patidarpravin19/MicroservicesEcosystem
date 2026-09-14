using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Persistence;

/// <summary>
/// Abstraction the audit interceptor uses to discover "who" performed a change,
/// without the interceptor depending directly on IHttpContextAccessor. Shared by
/// every service's DbContext instead of each redefining an identical copy.
/// </summary>
public interface ICurrentUserProvider
{
    string? UserId { get; }
}

public sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public string? UserId =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}
