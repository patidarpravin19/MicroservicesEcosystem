using BuildingBlocks.Contracts;
using InventoryService.Application.Abstractions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application.Consumers;

/// <summary>
/// Reacts to TenantService creating a new tenant by provisioning that tenant's own
/// PostgreSQL schema (and running InventoryService's migrations into it) so
/// CreateStockItemCommand/AddStockCommand work immediately for it. No roles/
/// permissions to seed here — those are entirely IdentityService's concern.
/// </summary>
public sealed class TenantProvisioningConsumer(
    ITenantSchemaProvisioner schemaProvisioner, ILogger<TenantProvisioningConsumer> logger)
    : IConsumer<TenantCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<TenantCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        await schemaProvisioner.ProvisionAsync(message.TenantId, message.SchemaName, context.CancellationToken);

        logger.LogInformation(
            "Tenant {TenantId} ({Name}) schema {SchemaName} provisioned in InventoryService.",
            message.TenantId, message.Name, message.SchemaName);
    }
}
