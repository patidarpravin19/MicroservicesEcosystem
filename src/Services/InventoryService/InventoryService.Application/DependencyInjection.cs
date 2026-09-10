using System.Reflection;
using FluentValidation;
using InventoryService.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryService.Application;

/// <summary>
/// Single entry point that registers everything the Application layer owns: MediatR
/// handlers, FluentValidation validators, and the pipeline behaviors (validation runs
/// first, then caching wraps the actual handler invocation).
///
/// To clone this Gold Master for a new service, copy this file and change only the
/// namespace — the registration pattern itself never needs to change.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return services;
    }

    /// <summary>
    /// Returns the Application assembly so Infrastructure can register MassTransit
    /// consumers (like StockAddedEventConsumer) by scanning it, without Infrastructure
    /// needing a hardcoded reference to every consumer type.
    /// </summary>
    public static Assembly ApplicationAssembly => Assembly.GetExecutingAssembly();
}
