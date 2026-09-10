using IdentityService.Application.Abstractions;
using IdentityService.Application.Common.Exceptions;
using IdentityService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Users.Commands.Register;

public sealed class RegisterCommandHandler(
    IIdentityDbContext db,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var exists = await db.Users.AnyAsync(
            u => u.UserName == request.UserName || u.Email == request.Email, cancellationToken);

        if (exists)
        {
            logger.LogWarning(
                "Registration rejected: username or email already in use ({UserName}, {Email}).",
                request.UserName, request.Email);
            throw new ConflictException("A user with that username or email already exists.");
        }

        var user = User.Create(request.UserName, request.Email, passwordHash: "pending");
        var hashed = passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(hashed);

        var pair = tokenService.GenerateTokenPair(user.Id, user.UserName, user.Roles);
        user.SetRefreshToken(tokenService.HashRefreshToken(pair.RefreshToken), DateTimeOffset.UtcNow.AddDays(7));

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("User {UserId} ({UserName}) registered successfully.", user.Id, user.UserName);

        return new RegisterResult(user.Id, pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc);
    }
}
