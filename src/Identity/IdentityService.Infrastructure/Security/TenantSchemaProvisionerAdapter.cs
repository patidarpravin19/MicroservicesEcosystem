using BuildingBlocks.Persistence;
using IdentityService.Application.Abstractions;
using IdentityService.Infrastructure.Persistence;

namespace IdentityService.Infrastructure.Security;

/// <summary>Thin adapter binding the shared generic provisioner to IdentityDbContext.</summary>
public sealed class TenantSchemaProvisionerAdapter(TenantSchemaProvisioner<IdentityDbContext> inner)
    : IdentityService.Application.Abstractions.ITenantSchemaProvisioner
{
    public Task ProvisionAsync(Guid tenantId, string schemaName, CancellationToken cancellationToken)
        => inner.ProvisionAsync(tenantId, schemaName, cancellationToken);
}
