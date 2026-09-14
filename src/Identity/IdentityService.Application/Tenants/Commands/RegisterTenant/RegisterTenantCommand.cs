using MediatR;

namespace IdentityService.Application.Tenants.Commands.RegisterTenant;

/// <summary>Self-service tenant signup — deliberately anonymous at the API layer
/// (see TenantEndpoints); add rate-limiting/CAPTCHA/email verification in front of
/// this in a production deployment.</summary>
public sealed record RegisterTenantCommand(string Name, string Slug) : IRequest<RegisterTenantResult>;

public sealed record RegisterTenantResult(
    Guid TenantId, string Name, string Slug, string SchemaName, string Status);
