using BuildingBlocks.Messaging;
using BuildingBlocks.Observability;
using BuildingBlocks.Observability.Middleware;
using BuildingBlocks.Security;
using BuildingBlocks.WebDefaults;
using InventoryService.Api.Endpoints;
using InventoryService.Application;
using InventoryService.Infrastructure;
using InventoryService.Infrastructure.Persistence;
using Serilog;

const string ServiceName = "InventoryService.Api";
var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging(ServiceName);

builder.Services.AddPlatformSecurity(builder.Configuration);
builder.Services.AddWebDefaults(ServiceName);
builder.Services.AddPlatformMessaging(builder.Configuration, InventoryService.Application.DependencyInjection.ApplicationAssembly);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseWebDefaults();
app.UseCorrelationId();
app.UseSharedRequestLogging();
app.UsePlatformSecurity();

app.MapStockEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

Log.Information("Starting {Service}", ServiceName);
app.Run();
