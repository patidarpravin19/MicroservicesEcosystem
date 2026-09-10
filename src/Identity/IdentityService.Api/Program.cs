using BuildingBlocks.Observability;
using BuildingBlocks.Observability.Middleware;
using IdentityService.Api.Endpoints;
using IdentityService.Api.ExceptionHandling;
using IdentityService.Application;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

const string ServiceName = "IdentityService.Api";

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging(ServiceName);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddOtlpExporter());

var app = builder.Build();

app.UseExceptionHandler(_ => { });
app.UseCorrelationId();
app.UseSharedRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
}

Log.Information("Starting {Service}", ServiceName);
app.Run();
