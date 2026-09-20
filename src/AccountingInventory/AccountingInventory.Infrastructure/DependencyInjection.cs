using BuildingBlocks.Persistence;
using BuildingBlocks.Security;
using AccountingInventory.Application.Abstractions;
using AccountingInventory.Infrastructure.Persistence;
using AccountingInventory.Infrastructure.Persistence.MultiTenancy;
using AccountingInventory.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AccountingInventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // "AccountingInventoryDb": one PostgreSQL database, multiple schemas.
        //   - "tenant" schema        → TenantDbContext: the shared tenant registry (master data)
        //   - "tenant_<name>_<id>"    → IdentityDbContext: Users/Roles, one schema per tenant,
        //                               named from that tenant's name + id (see
        //                               Tenant.Create / TenantSchemaNameValidator)
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantProvider, DynamicTenantProvider>();
        services.AddScoped<TenantSchemaConnectionInterceptor>();
        services.AddDbContext<AccountingInventoryDbContext>((sp, options) =>
        {
            var tenantProvider = sp.GetRequiredService<ITenantProvider>();

            var connectionString = configuration.GetConnectionString("AccountingInventoryDb")
                ?? throw new InvalidOperationException("The AccountingInventoryDb connection string is not configured.");
            var tenantConnectionString = TenantSchemaMigrator.CreateTenantConnectionString(connectionString, tenantProvider.SchemaName);

            options.UseNpgsql(
                       tenantConnectionString,
                   npgsql => npgsql.MigrationsHistoryTable("__TenantSchemaHistory", tenantProvider.SchemaName))
                   .UseSnakeCaseNamingConvention()
                   .ReplaceService<IModelCacheKeyFactory, TenantModelCacheKeyFactory>()
                   .AddInterceptors(sp.GetRequiredService<TenantSchemaConnectionInterceptor>());
        });
        services.AddScoped<IAccountingInventoryDbContext>(sp => sp.GetRequiredService<AccountingInventoryDbContext>());

        services.AddControlPlaneDbContext<TenantDbContext>(configuration, connectionStringName: "AccountingInventoryDb", schema: "tenant");
        services.AddScoped<ITenantDirectoryContext>(sp => sp.GetRequiredService<TenantDbContext>());

        services.AddScoped<ITenantSchemaProvisioner, TenantSchemaProvisionerAdapter>();

        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");
        services.AddSingleton(jwtOptions);
        services.AddSingleton<ITokenService, TokenService>();

        return services;
    }
}
