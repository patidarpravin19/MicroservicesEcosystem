using MediatR;

namespace IdentityService.Application.Users.Commands.Login;

public sealed record LoginCommand(string UserName, string Password) : IRequest<LoginResult>;

public sealed record LoginResult(
    Guid UserId,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);
