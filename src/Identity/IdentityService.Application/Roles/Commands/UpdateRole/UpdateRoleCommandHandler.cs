using BuildingBlocks.Application.Exceptions;
using IdentityService.Application.Abstractions;
using IdentityService.Application.Roles.Commands.CreateRole;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Roles.Commands.UpdateRole;

public sealed class UpdateRoleCommandHandler(IIdentityDbContext db, ILogger<UpdateRoleCommandHandler> logger)
    : IRequestHandler<UpdateRoleCommand, RoleResult>
{
    public async Task<RoleResult> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.SingleOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken)
            ?? throw new NotFoundException($"No role exists with id '{request.RoleId}'.");

        if (role.IsSystemDefined)
        {
            throw new ConflictException($"Role '{role.Name}' is system-defined and cannot be modified.");
        }

        var toRevoke = role.PermissionCodes.Except(request.PermissionCodes).ToList();
        var toGrant = request.PermissionCodes.Except(role.PermissionCodes).ToList();

        foreach (var code in toRevoke)
        {
            role.RevokePermission(code);
        }

        foreach (var code in toGrant)
        {
            role.GrantPermission(code);
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Role {RoleId} ({RoleName}) updated: +[{Granted}] -[{Revoked}].",
            role.Id, role.Name, string.Join(", ", toGrant), string.Join(", ", toRevoke));

        return new RoleResult(role.Id, role.Name, role.IsSystemDefined, [.. role.PermissionCodes]);
    }
}
