using MediatR;
using IdentityService.Application.Tenants.Queries.GetTenantBySlug;

namespace IdentityService.Application.Tenants.Queries.GetTenants;

public sealed record GetTenantsQuery : IRequest<IReadOnlyList<TenantSummary>>;
