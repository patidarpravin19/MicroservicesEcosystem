using BuildingBlocks.Persistence;
using BuildingBlocks.Security;
using IdentityService.Application.Abstractions;
using IdentityService.Infrastructure.Persistence;
using IdentityService.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // "EcosystemDb": one PostgreSQL database, multiple schemas.
        //   - "tenant" schema        → TenantDbContext: the shared tenant registry (master data)
        //   - "tenant_<name>_<id>"    → IdentityDbContext: Users/Roles, one schema per tenant,
        //                               named from that tenant's name + id (see
        //                               Tenant.Create / TenantSchemaNameValidator)
        services.AddTenantScopedDbContext<IdentityDbContext>(configuration, connectionStringName: "EcosystemDb");
        services.AddScoped<IIdentityDbContext>(sp => sp.GetRequiredService<IdentityDbContext>());

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