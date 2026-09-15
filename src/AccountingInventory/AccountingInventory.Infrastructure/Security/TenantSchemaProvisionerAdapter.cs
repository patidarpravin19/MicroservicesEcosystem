using BuildingBlocks.Persistence;
using AccountingInventory.Application.Abstractions;
using AccountingInventory.Infrastructure.Persistence;

namespace AccountingInventory.Infrastructure.Security;

/// <summary>Thin adapter binding the shared generic provisioner to IdentityDbContext.</summary>
public sealed class TenantSchemaProvisionerAdapter(TenantSchemaProvisioner<AccountingInventoryDbContext> inner)
    : AccountingInventory.Application.Abstractions.ITenantSchemaProvisioner
{
    public Task ProvisionAsync(Guid tenantId, string schemaName, CancellationToken cancellationToken)
        => inner.ProvisionAsync(tenantId, schemaName, cancellationToken);
}
