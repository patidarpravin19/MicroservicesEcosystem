using MediatR;

namespace IdentityService.Application.Users.Commands.AssignRole;

public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : IRequest;
