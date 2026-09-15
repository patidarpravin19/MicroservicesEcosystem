using BuildingBlocks.Persistence;
using BuildingBlocks.Security;
using AccountingInventory.Application.Abstractions;
using AccountingInventory.Infrastructure.Persistence;
using AccountingInventory.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // "EcosystemDb": one PostgreSQL database, multiple schemas.
        //   - "tenant" schema        → TenantDbContext: the shared tenant registry (master data)
        //   - "tenant_<name>_<id>"    → IdentityDbContext: Users/Roles, one schema per tenant,
        //                               named from that tenant's name + id (see
        //                               Tenant.Create / TenantSchemaNameValidator)
        services.AddTenantScopedDbContext<AccountingInventoryDbContext>(configuration, connectionStringName: "EcosystemDb");
        services.AddScoped<IAccountingInventoryDbContext>(sp => sp.GetRequiredService<AccountingInventoryDbContext>());

        services.AddControlPlaneDbContext<TenantDbContext>(configuration, connectionStringName: "EcosystemDb", schema: "tenant");
        services.AddScoped<ITenantDirectoryContext>(sp => sp.GetRequiredService<TenantDbContext>());

        services.AddScoped<ITenantSchemaProvisioner, TenantSchemaProvisionerAdapter>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        services.AddSingleton(jwtOptions);
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}