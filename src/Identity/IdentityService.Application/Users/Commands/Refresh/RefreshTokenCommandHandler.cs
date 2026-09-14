using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.MultiTenancy;
using IdentityService.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Users.Commands.Refresh;

/// <summary>
/// Issues a new access/refresh token pair without requiring the caller to know their
/// tenant slug again — the expired access token already carries tenant_id and
/// tenant_schema claims from when it was first minted (see ITokenService.
/// GetPrincipalFromExpiredToken), so this is the one Identity flow that resolves
/// tenant context directly from a token rather than a TenantDirectory lookup.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IIdentityDbContext db,
    ITokenService tokenService,
    ITenantContextAccessor tenantContextAccessor,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = tokenService.GetPrincipalFromExpiredToken(request.ExpiredAccessToken);

        if (principal is null)
        {
            logger.LogWarning("Token refresh failed: expired access token was malformed or invalid.");
            throw new UnauthorizedException("The access token is malformed or invalid.");
        }

        await db.ResetConnectionAsync(cancellationToken);
        tenantContextAccessor.SetTenant(principal.TenantId, principal.TenantSchema);

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == principal.UserId, cancellationToken);

        if (user is null)
        {
            logger.LogWarning(
                "Token refresh failed: no user {UserId} in tenant {TenantId}.", principal.UserId, principal.TenantId);
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        var hashedIncoming = tokenService.HashRefreshToken(request.RefreshToken);

        if (!user.IsRefreshTokenValid(hashedIncoming, DateTimeOffset.UtcNow))
        {
            logger.LogWarning(
                "Token refresh failed: refresh token invalid or expired for user {UserId}.", user.Id);
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        // Re-read roles/permissions from the database on every refresh — this is the
        // mechanism by which a role or permission change made in the database (e.g.
        // an admin revoking a permission from a role) reaches an already-logged-in
        // user: within one access-token lifetime (15 minutes by default).
        var roles = await db.Roles
            .Where(r => user.RoleIds.Contains(r.Id))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var roleNames = roles.Select(r => r.Name).ToArray();
        var permissionCodes = roles.SelectMany(r => r.PermissionCodes).Distinct().ToArray();

        var pair = tokenService.GenerateTokenPair(
            user.Id, user.UserName, principal.TenantId, principal.TenantSchema, roleNames, permissionCodes);

        user.SetRefreshToken(tokenService.HashRefreshToken(pair.RefreshToken), DateTimeOffset.UtcNow.AddDays(7));

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Access token refreshed for user {UserId} ({UserName}).", user.Id, user.UserName);

        return new RefreshTokenResult(pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc);
    }
}
