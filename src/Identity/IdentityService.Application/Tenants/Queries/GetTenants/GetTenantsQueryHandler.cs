using IdentityService.Application.Abstractions;
using IdentityService.Application.Tenants.Queries.GetTenantBySlug;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Tenants.Queries.GetTenants;

public sealed class GetTenantsQueryHandler(ITenantDirectoryContext tenantDirectory)
    : IRequestHandler<GetTenantsQuery, IReadOnlyList<TenantSummary>>
{
    public async Task<IReadOnlyList<TenantSummary>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
        => await tenantDirectory.Tenants
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TenantSummary(t.Id, t.Name, t.Slug, t.Status.ToString()))
            .ToListAsync(cancellationToken);
}
