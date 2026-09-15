using MediatR;

namespace AccountingInventory.Application.Users.Commands.Refresh;

public sealed record RefreshTokenCommand(
    string ExpiredAccessToken,
    string RefreshToken) : IRequest<RefreshTokenResult>;

public sealed record RefreshTokenResult(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAtUtc);
