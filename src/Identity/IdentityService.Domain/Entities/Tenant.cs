using BuildingBlocks.Domain;
using IdentityService.Domain.Events;
using IdentityService.Domain.Exceptions;

namespace IdentityService.Domain.Entities;

/// <summary>
/// The tenant registry, living alongside User and Role as a first-class aggregate of
/// the Identity bounded context — not a separate microservice. This is deliberately
/// the ONE entity in this DbContext that is NOT schema-per-tenant (it IS the list of
/// tenants), so it's pinned to the fixed "public" schema (see TenantConfiguration)
/// and migrated independently of the schema-per-tenant Users/Roles tables — see
/// IIdentityDbContext / ITenantDirectoryContext for why that split exists.
/// </summary>
public sealed class Tenant : AggregateRoot
{
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string SchemaName { get; init; }
    public TenantStatus Status { get; private set; }

    public static Tenant Create(string name, string slug)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new IdentityDomainException("Tenant name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new IdentityDomainException("Tenant slug cannot be empty.");
        }

        var tenantId = Guid.NewGuid();
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var schemaName = TenantSchemaNameValidator.BuildSchemaName(name, tenantId);

        var tenant = new Tenant
        {
            Id = tenantId,
            Name = name.Trim(),
            Slug = normalizedSlug,
            SchemaName = schemaName,
            Status = TenantStatus.PendingProvisioning,
        };

        tenant.RaiseDomainEvent(new TenantCreatedDomainEvent(
            tenant.Id, tenant.Name, tenant.Slug, tenant.SchemaName, DateTimeOffset.UtcNow));

        return tenant;
    }

    /// <summary>
    /// Marks the tenant active. Unlike a design where provisioning happens
    /// asynchronously in another service, IdentityService provisions its OWN schema
    /// synchronously within CreateTenantCommandHandler (same database — no network
    /// hop, no eventual-consistency window), so activation happens immediately once
    /// that step succeeds. Other services provisioning their own schemas from the
    /// published integration event remains asynchronous, which is fine — those
    /// services simply aren't ready for this tenant until their own consumer runs,
    /// independent of whether Identity itself considers the tenant "active".
    /// </summary>
    public void Activate()
    {
        if (Status == TenantStatus.Suspended)
        {
            throw new IdentityDomainException("A suspended tenant must be explicitly reactivated by an operator.");
        }

        Status = TenantStatus.Active;
    }

    public void Suspend()
    {
        if (Status == TenantStatus.Suspended)
        {
            throw new IdentityDomainException($"Tenant '{Name}' is already suspended.");
        }

        Status = TenantStatus.Suspended;
    }

    public void Reactivate()
    {
        if (Status != TenantStatus.Suspended)
        {
            throw new IdentityDomainException($"Tenant '{Name}' is not suspended.");
        }

        Status = TenantStatus.Active;
    }

    private Tenant() { }
}
