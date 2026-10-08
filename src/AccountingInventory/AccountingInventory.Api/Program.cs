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
AccountingInventory.Api.ProductionConfiguration.Validate(builder);

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
app.MapFinanceVendorEndpoints();
app.MapBrandEndpoints();
app.MapVariantEndpoints();
app.MapColorEndpoints();
app.MapProductTypeEndpoints();
app.MapProductModelEndpoints();
app.MapProductEndpoints();
app.MapInventoryEndpoints();
app.MapPurchaseAccountingEndpoints();
app.MapSalesProductEndpoints();
app.MapSalesAccountingEndpoints();
app.MapSalesInvoiceEndpoints();
app.MapCustomerEndpoints();
app.MapCustomerBillSettingsEndpoints();
app.MapAuditLogEndpoints();
app.MapGeneralLedgerEndpoints();
app.MapAccountingExtensionEndpoints();
app.MapAccountingP0Endpoints();
app.MapTaxEndpoints();
app.MapDashboardEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));
app.MapGet("/health/ready", async (TenantDbContext db, CancellationToken ct) =>
{
    try
    {
        await db.Tenants.AsNoTracking().Take(1).Select(x => x.Id).ToListAsync(ct);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception) { return Results.StatusCode(StatusCodes.Status503ServiceUnavailable); }
});

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<TenantDbContext>().Database.MigrateAsync();
}

await app.Services.ApplyTenantSchemaMigrationsAsync();

Log.Information("Starting {Service}", ServiceName);
app.Run();
