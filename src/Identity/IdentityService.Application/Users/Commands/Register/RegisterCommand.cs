using MediatR;

namespace IdentityService.Application.Users.Commands.Register;

public sealed record RegisterCommand(
    string UserName,
    string Email,
    string Password) : IRequest<RegisterResult>;

public sealed record RegisterResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);
