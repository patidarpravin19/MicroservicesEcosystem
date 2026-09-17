using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Domain.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Api.Endpoints;

/// <summary>
/// Establishes the tenant context for tenant-scoped endpoints from the required
/// <c>X-Tenant-Id</c> request header. The schema is read only from the control-plane
/// tenant registry, never from a client header.
/// </summary>
public sealed class TenantHeaderEndpointFilter : IEndpointFilter
{
    public const string TenantIdHeader = "X-Tenant-Id";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var values = context.HttpContext.Request.Headers[TenantIdHeader];
        if (values.Count != 1 || !Guid.TryParse(values[0], out var tenantId))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: $"Request header '{TenantIdHeader}' must contain one valid tenant id.");
        }

        var tenantContext = context.HttpContext.RequestServices.GetRequiredService<ITenantContext>();
        if (tenantContext.TenantId is { } authenticatedTenantId && authenticatedTenantId != tenantId)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "The tenant header does not match the authenticated user's tenant.");
        }

        var tenantDirectory = context.HttpContext.RequestServices.GetRequiredService<ITenantDirectoryContext>();
        var tenant = await tenantDirectory.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(t => t.Id == tenantId, context.HttpContext.RequestAborted);

        if (tenant is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Tenant was not found.");
        }

        if (tenant.Status != TenantStatus.Active || !tenant.IsActive)
        {
            return Results.Problem(statusCode: StatusCodes.Status403Forbidden, title: "Tenant is not active.");
        }

        context.HttpContext.RequestServices.GetRequiredService<ITenantContextAccessor>()
            .SetTenant(tenant.Id, tenant.SchemaName);

        return await next(context);
    }
}
