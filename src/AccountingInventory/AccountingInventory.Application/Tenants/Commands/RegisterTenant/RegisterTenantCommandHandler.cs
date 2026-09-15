using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Contracts;
using BuildingBlocks.Domain.MultiTenancy;
using BuildingBlocks.Messaging;
using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using AccountingInventory.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Tenants.Commands.RegisterTenant;

/// <summary>
/// The entire tenant onboarding flow, in one handler, entirely inside AccountingInventory:
///
///   1. Reserve the tenant's slug in the shared tenant registry ("TenantDb").
///   2. Provision a brand-new PostgreSQL schema for it — named from the tenant's
///      name AND id (e.g. "tenant_acme_corp_3f2a1b4c") — inside the "EcosystemDb"
///      database, and run Users/Roles migrations into that schema.
///   3. Seed the tenant's two default, system-defined roles ("Admin" with every
///      built-in permission, "User" with none) directly into that new schema.
///   4. Mark the tenant Active and persist that back to the registry.
///   5. Publish TenantCreatedIntegrationEvent so every OTHER tenant-scoped service
///      (InventoryService, and any future Gold-Master clone) provisions its own
///      schema for this tenant asynchronously.
///
/// Steps 1-4 are synchronous and same-process — no network hop, no eventual-
/// consistency window — because Identity owns both the tenant registry and the
/// Users/Roles schema itself. Only OTHER services' provisioning is asynchronous.
/// </summary>
public sealed class RegisterTenantCommandHandler(
    ITenantDirectoryContext tenantDirectory,
    IAccountingInventoryDbContext db,
    ITenantSchemaProvisioner schemaProvisioner,
    ITenantContextAccessor tenantContextAccessor,
    IEventPublisher eventPublisher,
    ILogger<RegisterTenantCommandHandler> logger)
    : IRequestHandler<RegisterTenantCommand, RegisterTenantResult>
{
    private static readonly string[] DefaultAdminPermissions =
    [
        "Roles.Manage",
        "Users.Manage",
        "Inventory.StockItems.Create",
        "Inventory.StockItems.Read",
        "Inventory.StockItems.AddStock",
    ];

    public async Task<RegisterTenantResult> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        var slugTaken = await tenantDirectory.Tenants.AnyAsync(t => t.Slug == normalizedSlug, cancellationToken);

        if (slugTaken)
        {
            logger.LogWarning("Tenant registration rejected: slug {Slug} already in use.", normalizedSlug);
            throw new ConflictException($"A tenant with slug '{normalizedSlug}' already exists.");
        }

        var tenant = Tenant.Create(request.Name, normalizedSlug);

        tenantDirectory.Tenants.Add(tenant);
        await tenantDirectory.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Tenant {TenantId} ({Name}) reserved with schema {SchemaName}; provisioning...",
            tenant.Id, tenant.Name, tenant.SchemaName);

        // From here on, every query against IIdentityDbContext (db.Users / db.Roles)
        // targets the new tenant's schema — TenantSchemaProvisioner sets that up as
        // part of ProvisionAsync (creates the schema, points ITenantContext at it,
        // and runs migrations), so no further ResetConnectionAsync/SetTenant call is
        // needed here.
        await schemaProvisioner.ProvisionAsync(tenant.Id, tenant.SchemaName, cancellationToken);

        var adminRole = Role.Create(tenant.Id, "Admin", isSystemDefined: true);
        foreach (var permission in DefaultAdminPermissions)
        {
            adminRole.GrantPermission(permission);
        }

        var userRole = Role.Create(tenant.Id, "User", isSystemDefined: true);

        db.Roles.Add(adminRole);
        db.Roles.Add(userRole);
        await db.SaveChangesAsync(cancellationToken);

        tenant.Activate();
        await tenantDirectory.SaveChangesAsync(cancellationToken);

        // TODO to enable integration events, uncomment the following lines and implement the event publishing logic
        //var domainEvent = tenant.DomainEvents.OfType<TenantCreatedDomainEvent>().Single();
        //tenant.ClearDomainEvents();

        //await eventPublisher.PublishAsync(
        //    new TenantCreatedIntegrationEvent(
        //        domainEvent.TenantId, domainEvent.Name, domainEvent.Slug, domainEvent.SchemaName, domainEvent.OccurredOnUtc),
        //    cancellationToken);

        logger.LogInformation(
            "Tenant {TenantId} ({Name}) fully provisioned and activated: schema {SchemaName}, default roles Admin/User seeded.",
            tenant.Id, tenant.Name, tenant.SchemaName);

        return new RegisterTenantResult(tenant.Id, tenant.Name, tenant.Slug, tenant.SchemaName, tenant.Status.ToString());
    }
}
