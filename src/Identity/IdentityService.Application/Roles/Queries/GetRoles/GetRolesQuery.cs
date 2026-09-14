using IdentityService.Application.Roles.Commands.CreateRole;
using MediatR;

namespace IdentityService.Application.Roles.Queries.GetRoles;

public sealed record GetRolesQuery : IRequest<IReadOnlyList<RoleResult>>;
