using IdentityService.Application.Abstractions;
using IdentityService.Application.Common.Exceptions;
using IdentityService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Users.Commands.Login;

public sealed class LoginCommandHandler(
    IIdentityDbContext db,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Login failed: no user found for username {UserName}.", request.UserName);
            throw new UnauthorizedException("Invalid username or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            logger.LogWarning(
                "Login failed: bad password for user {UserId} ({UserName}).", user.Id, user.UserName);
            throw new UnauthorizedException("Invalid username or password.");
        }

        var pair = tokenService.GenerateTokenPair(user.Id, user.UserName, user.Roles);
        user.SetRefreshToken(tokenService.HashRefreshToken(pair.RefreshToken), DateTimeOffset.UtcNow.AddDays(7));

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} ({UserName}) logged in successfully.", user.Id, user.UserName);

        return new LoginResult(user.Id, pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc);
    }
}
