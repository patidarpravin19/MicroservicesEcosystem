using MediatR;

namespace IdentityService.Application.Roles.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId) : IRequest;
