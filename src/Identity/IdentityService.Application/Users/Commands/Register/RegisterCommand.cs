using MediatR;

namespace IdentityService.Application.Users.Commands.Register;

public sealed record RegisterCommand(
    string TenantSlug,
    string UserName,
    string Email,
    string Password) : IRequest<RegisterResult>;

public sealed record RegisterResult(
    Guid UserId,
    Guid TenantId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);
