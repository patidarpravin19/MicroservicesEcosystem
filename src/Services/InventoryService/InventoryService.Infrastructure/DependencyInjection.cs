using InventoryService.Application.Abstractions;
using InventoryService.Infrastructure.Caching;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Persistence;
using InventoryService.Infrastructure.Persistence.Interceptors;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryService.Infrastructure;

/// <summary>
/// Single entry point that registers everything the Infrastructure layer owns:
/// PostgreSQL via EF Core with the audit interceptor, Redis distributed caching,
/// and MassTransit/RabbitMQ with retry + dead-letter-queue semantics and automatic
/// consumer discovery from the Application assembly.
///
/// To clone this Gold Master for a new service, copy this file, change only the
/// DbContext type parameter and the connection-string/queue-prefix names — every
/// cross-cutting behavior (auditing, caching, messaging resilience) is inherited
/// unchanged.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserProvider, HttpCurrentUserProvider>();
        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        // --- PostgreSQL / EF Core 10 ---
        services.AddDbContext<InventoryDbContext>((sp, options) =>
            options.UseNpgsql(configuration.GetConnectionString("InventoryDb")));

        services.AddScoped<IInventoryDbContext>(sp => sp.GetRequiredService<InventoryDbContext>());

        // --- Redis distributed cache ---
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
            options.InstanceName = "inventory:";
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        // --- MassTransit / RabbitMQ ---
        var rabbitMqOptions = configuration.GetSection(RabbitMqOptions.SectionName).Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMq configuration section is missing.");

        services.AddScoped<IEventPublisher, EventPublisher>();

        services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.AddConsumers(Application.DependencyInjection.ApplicationAssembly);

            busConfigurator.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqOptions.Host, rabbitMqOptions.VirtualHost, h =>
                {
                    h.Username(rabbitMqOptions.Username);
                    h.Password(rabbitMqOptions.Password);
                });

                cfg.UsePublishFilter(typeof(CorrelationIdPublishFilter<>), context);

                // Automatic retry with exponential backoff, then route to a
                // per-queue dead-letter exchange/queue after exhausting retries.
                cfg.UseMessageRetry(retry => retry.Exponential(
                    retryLimit: 5,
                    minInterval: TimeSpan.FromSeconds(1),
                    maxInterval: TimeSpan.FromSeconds(30),
                    intervalDelta: TimeSpan.FromSeconds(5)));

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
