namespace IdentityService.Application.Abstractions;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc);

/// <summary>
/// Dependency-inversion seam for JWT issuance. Implemented in Infrastructure so the
/// Application layer never references System.IdentityModel.Tokens.Jwt directly.
/// </summary>
public interface ITokenService
{
    TokenPair GenerateTokenPair(Guid userId, string userName, IEnumerable<string> roles);

    string HashRefreshToken(string refreshToken);

    Guid? GetUserIdFromExpiredAccessToken(string expiredAccessToken);
}
