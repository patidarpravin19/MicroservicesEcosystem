using IdentityService.Application.Abstractions;
using IdentityService.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Users.Commands.Refresh;

public sealed class RefreshTokenCommandHandler(
    IIdentityDbContext db,
    ITokenService tokenService,
    ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var userId = tokenService.GetUserIdFromExpiredAccessToken(request.ExpiredAccessToken);

        if (userId is null)
        {
            logger.LogWarning("Token refresh failed: expired access token was malformed or invalid.");
            throw new UnauthorizedException("The access token is malformed or invalid.");
        }

        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Token refresh failed: no user found for id {UserId}.", userId);
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        var hashedIncoming = tokenService.HashRefreshToken(request.RefreshToken);

        if (!user.IsRefreshTokenValid(hashedIncoming, DateTimeOffset.UtcNow))
        {
            logger.LogWarning(
                "Token refresh failed: refresh token invalid or expired for user {UserId}.", user.Id);
            throw new UnauthorizedException("Refresh token is invalid or expired.");
        }

        var pair = tokenService.GenerateTokenPair(user.Id, user.UserName, user.Roles);
        user.SetRefreshToken(tokenService.HashRefreshToken(pair.RefreshToken), DateTimeOffset.UtcNow.AddDays(7));

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Access token refreshed for user {UserId} ({UserName}).", user.Id, user.UserName);

        return new RefreshTokenResult(pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc);
    }
}
