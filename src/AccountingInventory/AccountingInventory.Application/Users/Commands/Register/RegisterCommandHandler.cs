using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.MultiTenancy;
using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Users.Commands.Register;

/// <summary>
/// Creates the first user in an empty tenant or an additional account requested
/// by the existing owner. The API establishes tenant context before this handler.
/// </summary>
public sealed class RegisterCommandHandler(
    IAccountingInventoryDbContext db,
    ITenantContext tenantContext,
    IRequestIdentity identity,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.TenantId
            ?? throw new InvalidOperationException("A tenant must be established before registering a user.");
        var schemaName = tenantContext.SchemaName
            ?? throw new InvalidOperationException("The established tenant does not have a schema.");

        var owner = await db.Users.AsNoTracking().OrderBy(user => user.CreatedAt).ThenBy(user => user.Id)
            .Select(user => (Guid?)user.Id).FirstOrDefaultAsync(cancellationToken);
        if (owner.HasValue && identity.UserId != owner)
            throw new ForbiddenException("Only the tenant owner can create additional user accounts.");

        var userName = request.UserName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var mobile = request.Mobile.Trim();
        var exists = await db.Users.AnyAsync(
            u => u.UserName.ToLower() == userName.ToLower() || u.Email.ToLower() == email, cancellationToken);

        if (exists)
        {
            logger.LogWarning(
                "Registration rejected for tenant {TenantId}: username or email already in use ({UserName}, {Email}).",
                tenantId, request.UserName, request.Email);
            throw new ConflictException("A user with that username or email already exists.");
        }

        var user = User.Create(userName, mobile, email);
        user.Activate();
        var hashed = passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(hashed);

        var pair = tokenService.GenerateTokenPair(
            user.Id, user.UserName, tenantId, schemaName,
            roleNames: [],
            permissionCodes: []);

        user.SetRefreshToken(tokenService.HashRefreshToken(pair.RefreshToken), DateTimeOffset.UtcNow.AddDays(7));

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} ({UserName}) registered for tenant {TenantId}.", user.Id, user.UserName, tenantId);

        return new RegisterResult(user.Id, tenantId, pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc);
    }
}
