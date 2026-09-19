using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain.MultiTenancy;
using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Users.Commands.Login;

public sealed class LoginCommandHandler(
    IAccountingInventoryDbContext accInvDbContext,
    ITenantDirectoryContext tenantDirectory,
     ITenantContext tenantContext,
    ITokenService tokenService,
    ITenantContextAccessor tenantContextAccessor,
    IPasswordHasher<User> passwordHasher,
    ILogger<LoginCommandHandler> logger)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedSlug = request.TenantSlug.Trim().ToLowerInvariant();

        var tenant = await tenantDirectory.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Slug == normalizedSlug, cancellationToken);

        if (tenant is null)
        {
            logger.LogWarning("Login failed: unknown tenant slug {TenantSlug}.", normalizedSlug);
            throw new UnauthorizedException("Invalid tenant, username, or password.");
        }

        if (tenant.Status != TenantStatus.Active)
        {
            logger.LogWarning("Login failed: tenant {TenantId} is not active ({Status}).", tenant.Id, tenant.Status);
            throw new UnauthorizedException("This tenant is not currently active.");
        }

        await accInvDbContext.ResetConnectionAsync(cancellationToken);
        tenantContextAccessor.SetTenant(tenant.Id, tenant.SchemaName);
       
        var user = await accInvDbContext.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName, cancellationToken);

        if (user is null)
        {
            logger.LogWarning("Login failed: no user {UserName} for tenant {TenantId}.", request.UserName, tenant.Id);
            throw new UnauthorizedException("Invalid tenant, username, or password.");
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            logger.LogWarning(
                "Login failed: bad password for user {UserId} ({UserName}) in tenant {TenantId}.",
                user.Id, user.UserName, tenant.Id);
            throw new UnauthorizedException("Invalid tenant, username, or password.");
        }

        //var roles = await db.Roles
        //    .Where(r => user.RoleIds.Contains(r.Id))
        //    .AsNoTracking()
        //    .ToListAsync(cancellationToken);

        var roleNames = new string[] { };
        var permissionCodes = new string[] { };

        var pair = tokenService.GenerateTokenPair(
            user.Id, user.UserName, tenant.Id, tenant.SchemaName, roleNames, permissionCodes);

        user.SetRefreshToken(tokenService.HashRefreshToken(pair.RefreshToken), DateTimeOffset.UtcNow.AddDays(7));

        await accInvDbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "User {UserId} ({UserName}) logged in to tenant {TenantId} with roles [{Roles}].",
            user.Id, user.UserName, tenant.Id, string.Join(", ", roleNames));

        return new LoginResult(user.Id, tenant.Id, pair.AccessToken, pair.RefreshToken, pair.AccessTokenExpiresAtUtc);
    }
}
