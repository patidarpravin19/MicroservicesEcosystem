using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using BuildingBlocks.Observability.Middleware;
using BuildingBlocks.Security;
using BuildingBlocks.WebDefaults;
using AccountingInventory.Api.Endpoints;
using AccountingInventory.Application;
using AccountingInventory.Infrastructure;
using AccountingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string ServiceName = "AccountingInventory.Api";
var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging(ServiceName);

builder.Services.AddPlatformSecurity(builder.Configuration);
builder.Services.AddWebDefaults(ServiceName);
builder.Services.AddPlatformMessaging(builder.Configuration, AccountingInventory.Application.DependencyInjection.ApplicationAssembly);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseWebDefaults();
app.UseCorrelationId();
app.UseSharedRequestLogging();
app.UsePlatformSecurity();

app.MapAuthEndpoints();
//app.MapRoleEndpoints();
app.MapTenantEndpoints();
app.MapVendorEndpoints();
app.MapBrandEndpoints();
app.MapProductTypeEndpoints();
app.MapProductModelEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<TenantDbContext>().Database.MigrateAsync();
}

await app.Services.ApplyTenantSchemaMigrationsAsync();

Log.Information("Starting {Service}", ServiceName);
app.Run();
