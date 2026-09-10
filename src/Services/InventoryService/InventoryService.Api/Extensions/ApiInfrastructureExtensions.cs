using System.Text;
using BuildingBlocks.Observability;
using BuildingBlocks.Observability.Middleware;
using InventoryService.Api.ExceptionHandling;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace InventoryService.Api.Extensions;

/// <summary>
/// Bundles every API-layer cross-cutting registration (JWT auth, Swagger,
/// OpenTelemetry, global exception handling) behind two calls so Program.cs stays
/// under 20 lines of actual configuration code — the defining trait of this Gold
/// Master template. A new service cloned from InventoryService only ever edits
/// Endpoints/, Application/StockItems-equivalent slices, and this file's JWT/service
/// name constants; the shape never changes.
/// </summary>
public static class ApiInfrastructureExtensions
{
    public static IServiceCollection AddApiInfrastructure(
        this IServiceCollection services, IConfiguration configuration, string serviceName)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey configuration is missing.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy("AuthenticatedUser", p => p.RequireAuthenticatedUser());

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("MassTransit")
                .AddOtlpExporter())
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());

        return services;
    }

    public static WebApplication UseApiInfrastructure(this WebApplication app, IWebHostEnvironment environment)
    {
        app.UseExceptionHandler(_ => { });
        app.UseCorrelationId();
        app.UseSharedRequestLogging();

        if (environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
