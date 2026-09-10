using BuildingBlocks.Observability;
using InventoryService.Api.Endpoints;
using InventoryService.Api.Extensions;
using InventoryService.Application;
using InventoryService.Infrastructure;
using InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

const string ServiceName = "InventoryService.Api";
var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging(ServiceName);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiInfrastructure(builder.Configuration, ServiceName);

var app = builder.Build();

app.UseApiInfrastructure(app.Environment);
app.MapStockEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<InventoryDbContext>().Database.Migrate();
}

Log.Information("Starting {Service}", ServiceName);
app.Run();
