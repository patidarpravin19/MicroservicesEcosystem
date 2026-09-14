namespace InventoryService.Application.Abstractions;

/// <summary>Dependency-inversion seam over BuildingBlocks.Persistence's generic
/// TenantSchemaProvisioner — see IdentityService's identical abstraction for the full
/// rationale.</summary>
public interface ITenantSchemaProvisioner
{
    Task ProvisionAsync(Guid tenantId, string schemaName, CancellationToken cancellationToken);
}
