using BuildingBlocks.Observability.Middleware;
using MassTransit;
using Microsoft.AspNetCore.Http;

namespace InventoryService.Infrastructure.Messaging;

/// <summary>
/// MassTransit publish filter that copies the current HTTP request's X-Correlation-ID
/// header onto every outbound message header, so a trace can be followed from the
/// originating API call through RabbitMQ into any consumer — completing the
/// correlation story that starts at BuildingBlocks.Observability's
/// CorrelationIdMiddleware, shared by every API in the solution.
///
/// Registered generically in DependencyInjection via:
///   cfg.UsePublishFilter(typeof(CorrelationIdPublishFilter&lt;&gt;), context);
/// MassTransit resolves this filter's constructor dependencies (IHttpContextAccessor)
/// from the DI container automatically for every message type T.
/// </summary>
public sealed class CorrelationIdPublishFilter<T>(IHttpContextAccessor httpContextAccessor)
    : IFilter<PublishContext<T>> where T : class
{
    public Task Send(PublishContext<T> context, IPipe<PublishContext<T>> next)
    {
        var correlationId = CorrelationIdMiddleware.GetCurrentCorrelationId(httpContextAccessor.HttpContext);

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            context.Headers.Set(CorrelationIdMiddleware.HeaderName, correlationId);
        }

        return next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("correlationIdPublishFilter");
}
