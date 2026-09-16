using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BuildingBlocks.Security;
using AccountingInventory.Application.Abstractions;
using Microsoft.IdentityModel.Tokens;

namespace AccountingInventory.Infrastructure.Security;

/// <summary>
/// Concrete implementation of the Application layer's ITokenService abstraction.
/// Issues short-lived HS256 access tokens carrying tenant identity, role names, and
/// the union of every permission code those roles grant — computed by the caller
/// (LoginCommandHandler / RefreshTokenCommandHandler) directly from the database
/// immediately before the token is minted, and opaque, hashed-at-rest refresh tokens.
/// </summary>
public sealed class TokenService(JwtOptions options) : ITokenService
{
    public TokenPair GenerateTokenPair(
        Guid userId,
        string userName,
        Guid tenantId,
        string tenantSchema,
        IEnumerable<string> roleNames,
        IEnumerable<string> permissionCodes)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(options.AccessTokenMinutes);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, userName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(TenantClaimTypes.TenantId, tenantId.ToString()),
            new(TenantClaimTypes.TenantSchema, tenantSchema),
        ];

        //claims.AddRange(roleNames.Select(role => new Claim(ClaimTypes.Role, role)));
        //claims.AddRange(permissionCodes.Select(code => new Claim(PermissionClaimTypes.Permission, code)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenPair(accessToken, refreshToken, expiresAt);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    public ExpiredTokenPrincipal? GetPrincipalFromExpiredToken(string expiredAccessToken)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidAudience = options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
            ValidateLifetime = false,
        };

        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(expiredAccessToken, validationParameters, out var securityToken);

            if (securityToken is not JwtSecurityToken jwt ||
                !jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            var tenantIdClaim = principal.FindFirst(TenantClaimTypes.TenantId)?.Value;
            var schemaClaim = principal.FindFirst(TenantClaimTypes.TenantSchema)?.Value;

            if (!Guid.TryParse(sub, out var userId) ||
                !Guid.TryParse(tenantIdClaim, out var tenantId) ||
                string.IsNullOrWhiteSpace(schemaClaim))
            {
                return null;
            }

            return new ExpiredTokenPrincipal(userId, tenantId, schemaClaim);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
    }
}
