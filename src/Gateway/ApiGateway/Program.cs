using BuildingBlocks.Observability;
using BuildingBlocks.Observability.Middleware;
using Serilog;
using System.Threading.RateLimiting;

const string ServiceName = "ApiGateway";

var builder = WebApplication.CreateBuilder(args);

builder.AddSharedLogging(ServiceName);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20,
            }));

    options.OnRejected = async (context, ct) =>
    {
        Log.Warning(
            "Rate limit exceeded for {RemoteIp} on {Path}. Correlation Id: {CorrelationId}",
            context.HttpContext.Connection.RemoteIpAddress,
            context.HttpContext.Request.Path,
            CorrelationIdMiddleware.GetCurrentCorrelationId(context.HttpContext));

        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title = "Too many requests. Please retry later.",
            status = 429,
        }, ct);
    };
});

var app = builder.Build();

app.UseCorrelationId();
app.UseSharedRequestLogging();
app.UseRateLimiter();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = ServiceName }));

app.MapReverseProxy(pipeline =>
{
    pipeline.Use(async (context, next) =>
    {
        if (context.Items[CorrelationIdMiddleware.HeaderName] is string correlationId)
        {
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = correlationId;
        }
        await next();
    });
});

try
{
    Log.Information("Starting {Service}", ServiceName);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "{Service} terminated unexpectedly", ServiceName);
}
finally
{
    Log.CloseAndFlush();
}
