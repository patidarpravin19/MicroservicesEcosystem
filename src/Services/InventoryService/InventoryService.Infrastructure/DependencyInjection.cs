using BuildingBlocks.Persistence;
using InventoryService.Application.Abstractions;
using InventoryService.Infrastructure.Caching;
using InventoryService.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryService.Infrastructure;

/// <summary>
/// Registers everything this service's Infrastructure layer owns: the schema-per-
/// tenant PostgreSQL DbContext (audit + tenant-schema interceptors wired up by
/// BuildingBlocks.Persistence), Redis caching, and the tenant schema provisioner.
/// JWT auth and the RabbitMQ bus are host-level concerns (BuildingBlocks.Security /
/// BuildingBlocks.Messaging), not registered here — see Program.cs.
///
/// To clone this Gold Master for a new service, copy this file and change only the
/// DbContext type parameter and connection-string name.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTenantScopedDbContext<InventoryDbContext>(configuration, connectionStringName: "InventoryDb");
        services.AddScoped<IInventoryDbContext>(sp => sp.GetRequiredService<InventoryDbContext>());

        services.AddScoped<ITenantSchemaProvisioner, TenantSchemaProvisionerAdapter>();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "inventory:";
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}
