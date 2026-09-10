using IdentityService.Domain.Common;
using IdentityService.Domain.Exceptions;

namespace IdentityService.Domain.Entities;

/// <summary>
/// Aggregate root for the Identity bounded context. Encapsulates password-hash storage,
/// role assignment, and refresh-token rotation behind explicit behavior methods so that
/// invariants (e.g. "a user always has at least one role") can never be violated from
/// outside the aggregate.
/// </summary>
public sealed class User : AuditableEntity
{
    private readonly List<string> _roles = [];

    public required string UserName { get; init; }
    public required string Email { get; init; }
    public string PasswordHash { get; private set; } = string.Empty;
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();
    public string? RefreshTokenHash { get; private set; }
    public DateTimeOffset? RefreshTokenExpiresAtUtc { get; private set; }

    public static User Create(string userName, string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            throw new IdentityDomainException("Username cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new IdentityDomainException("Email cannot be empty.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
        };

        user.PasswordHash = passwordHash;
        user._roles.Add("User");
        return user;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new IdentityDomainException("Password hash cannot be empty.");
        }

        PasswordHash = passwordHash;
    }

    public void SetRefreshToken(string refreshTokenHash, DateTimeOffset expiresAtUtc)
    {
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiresAtUtc = expiresAtUtc;
    }

    public bool IsRefreshTokenValid(string refreshTokenHash, DateTimeOffset nowUtc)
        => RefreshTokenHash == refreshTokenHash &&
           RefreshTokenExpiresAtUtc is not null &&
           RefreshTokenExpiresAtUtc > nowUtc;

    public void RevokeRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAtUtc = null;
    }

    public void PromoteToAdmin()
    {
        if (!_roles.Contains("Admin"))
        {
            _roles.Add("Admin");
        }
    }

    private User() { }
}
