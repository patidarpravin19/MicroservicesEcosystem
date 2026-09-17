using BuildingBlocks.Domain;
using BuildingBlocks.Domain.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace AccountingInventory.Infrastructure.Persistence.MultiTenancy;

/// <summary>
/// Resolves the tenant schema from the request header. Background work and requests
/// without a header use the supplied safe fallback schema.
/// </summary>
public sealed class DynamicTenantProvider(
    IHttpContextAccessor httpContextAccessor,
    ITenantContext tenantContext) : ITenantProvider
{
    public const string TenantSchemaHeader = "X-Tenant-Schema";
    public const string DefaultSchema = "public";

    public string SchemaName
    {
        get
        {
            var schema = httpContextAccessor.HttpContext?
                .Request.Headers[TenantSchemaHeader]
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(schema))
            {
                return TenantSchemaNameValidator.EnsureValid(schema);
            }

            // Supports non-HTTP flows that explicitly establish a tenant context,
            // such as registration/provisioning consumers.
            return tenantContext.SchemaName is { Length: > 0 } contextSchema
                ? TenantSchemaNameValidator.EnsureValid(contextSchema)
                : DefaultSchema;
        }
    }
}
