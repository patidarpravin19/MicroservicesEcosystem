using MediatR;

namespace IdentityService.Application.Users.Commands.RemoveRole;

public sealed record RemoveRoleCommand(Guid UserId, Guid RoleId) : IRequest;
