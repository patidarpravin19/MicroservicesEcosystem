using BuildingBlocks.Application.Exceptions;
using IdentityService.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Users.Commands.RemoveRole;

public sealed class RemoveRoleCommandHandler(IIdentityDbContext db, ILogger<RemoveRoleCommandHandler> logger)
    : IRequestHandler<RemoveRoleCommand>
{
    public async Task Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException($"No user exists with id '{request.UserId}'.");

        user.RemoveRole(request.RoleId);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Role {RoleId} removed from user {UserId}.", request.RoleId, user.Id);
    }
}
