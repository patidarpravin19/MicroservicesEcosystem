using BuildingBlocks.Persistence;
using InventoryService.Application.Abstractions;
using InventoryService.Infrastructure.Persistence;

namespace InventoryService.Infrastructure;

public sealed class TenantSchemaProvisionerAdapter(TenantSchemaProvisioner<InventoryDbContext> inner)
    : ITenantSchemaProvisioner
{
    public Task ProvisionAsync(Guid tenantId, string schemaName, CancellationToken cancellationToken)
        => inner.ProvisionAsync(tenantId, schemaName, cancellationToken);
}
