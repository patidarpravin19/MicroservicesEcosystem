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
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

// Only the tenant registry ("tenant" schema) migrates eagerly at startup — each
// tenant's own Users/Roles schema is created on demand by RegisterTenantCommandHandler
// via TenantSchemaProvisioner the moment that tenant registers. Both share the same
// "IdentityDb" database/connection string; only the schema differs.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<TenantDbContext>().Database.Migrate();
}

Log.Information("Starting {Service}", ServiceName);
app.Run();