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
/// Registers a new user for the active tenant established from <c>X-Tenant-Id</c>
/// by the API endpoint filter. Every new user is assigned the tenant's default "User"
/// role, seeded synchronously when the tenant was registered
/// (RegisterTenantCommandHandler) — so it is always present by the time anyone can
/// reach this handler.
/// </summary>
public sealed class RegisterCommandHandler(
    IAccountingInventoryDbContext db,
    ITenantContext tenantContext,
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

        var exists = await db.Users.AnyAsync(
            u => u.UserName == request.UserName || u.Email == request.Email, cancellationToken);

        if (exists)
        {
            logger.LogWarning(
                "Registration rejected for tenant {TenantId}: username or email already in use ({UserName}, {Email}).",
                tenantId, request.UserName, request.Email);
            throw new ConflictException("A user with that username or email already exists.");
        }

        //var defaultRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == DefaultRoleName, cancellationToken)
        //    ?? throw new ConflictException(
        //        "This tenant is still being provisioned. Please try registering again in a few seconds.");

        var user = User.Create(request.UserName, request.Email, request.Mobile);
        user.Activate();
        var hashed = passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(hashed);
        //user.AssignRole(defaultRole.Id);

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
