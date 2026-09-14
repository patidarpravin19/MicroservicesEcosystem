using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.MultiTenancy;
using IdentityService.Application.Abstractions;
using IdentityService.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Users.Commands.Register;

/// <summary>
/// Registers a new user for an existing, active tenant. The tenant is identified by
/// its slug, resolved through the shared tenant registry (ITenantDirectoryContext,
/// backed by the "TenantDb" database), since this endpoint runs before the caller has
/// any credentials at all. Every new user is assigned the tenant's default "User"
/// role, seeded synchronously when the tenant was registered
/// (RegisterTenantCommandHandler) — so it is always present by the time anyone can
/// reach this handler.
/// </summary>
public sealed class RegisterCommandHandler(
    IIdentityDbContext db,
    ITenantDirectoryContext tenantDirectory,
    ITokenService tokenService,
    IPasswordHasher<User> passwordHasher,
    ITenantContextAccessor tenantContextAccessor,
    ILogger<RegisterCommandHandler> logger)
    : IRequestHandler<RegisterCommand, RegisterResult>
{
    private const string DefaultRoleName = "User";

    public async Task<RegisterResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedSlug = request.TenantSlug.Trim().ToLowerInvariant();

        var tenant = await tenantDirectory.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Slug == normalizedSlug, cancellationToken)
            ?? throw new NotFoundException($"Unknown tenant '{normalizedSlug}'.");

        if (tenant.Status != TenantStatus.Active)
        {
            throw new ConflictException($"Tenant '{tenant.Name}' is not currently active.");
        }

        // Force a fresh connection so the schema we're about to set actually takes
        // effect for every query from here on — see IIdentityDbContext.ResetConnectionAsync.
        await db.ResetConnectionAsync(cancellationToken);
        tenantContextAccessor.SetTenant(tenant.Id, tenant.SchemaName);

        var exists = await db.Users.AnyAsync(
            u => u.UserName == request.UserName || u.Email == request.Email, cancellationToken);

        if (exists)
        {
            logger.LogWarning(
                "Registration rejected for tenant {TenantId}: username or email already in use ({UserName}, {Email}).",
                tenant.Id, request.UserName, request.Email);
            throw new ConflictException("A user with that username or email already exists.");
        }

        var defaultRole = await db.Roles.SingleOrDefaultAsync(r => r.Name == DefaultRoleName, cancellationToken)
            ?? throw new ConflictException(
                "This tenant is still being provisioned. Please try registering again in a few seconds.");

        var user = User.Create(tenant.Id, request.UserName, request.Email);
        var hashed = passwordHasher.HashPassword(user, request.Password);
        user.SetPasswordHash(hashed);
        user.AssignRole(defaultRole.Id);

        var pair = tokenService.GenerateTokenPair(
            user.Id, user.UserName, tenant.Id, tenant.SchemaName,
            roleNames: [defaultRole.Name],
            permissionCodes: defaultRole.PermissionCodes);

        user.SetRefreshToken(tokenService.HashRefreshToken(pair.RefreshToken), DateTimeOffset.UtcNow.AddDays(7));

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} ({UserName}) registered for tenant {TenantId}.", user.Id, user.UserName, tenant.Id);

        return new RegisterResult(user.Id, tenant.Id, pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc);
    }
}
