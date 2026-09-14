using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Domain;
using BuildingBlocks.Observability.Middleware;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.WebDefaults;

/// <summary>
/// The one .NET 10 IExceptionHandler implementation for every service in the
/// ecosystem. Converts FluentValidation failures, any service's DomainException
/// subclass, and the shared Conflict/NotFound/Unauthorized/Forbidden exceptions into
/// RFC 7807 ProblemDetails responses. Because every service's domain exceptions
/// derive from the same BuildingBlocks.Domain.DomainException base, and every
/// service's Application-layer exceptions ARE the same shared types (from
/// BuildingBlocks.Application.Exceptions), this single handler covers Identity,
/// Inventory, Tenant, and any future Gold-Master clone without modification —
/// feature code anywhere never needs its own try/catch.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdMiddleware.GetCurrentCorrelationId(httpContext) ?? httpContext.TraceIdentifier;

        var (statusCode, title, errors) = Map(exception);

        logger.LogError(
            exception,
            "Unhandled exception. CorrelationId={CorrelationId} StatusCode={StatusCode} Path={Path}",
            correlationId, statusCode, httpContext.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{statusCode}",
            Instance = httpContext.Request.Path,
            Detail = environment.IsDevelopment() ? exception.Message : title,
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (errors is not null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int StatusCode, string Title, IReadOnlyDictionary<string, string[]>? Errors) Map(Exception exception)
        => exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            DomainException domainException => (StatusCodes.Status400BadRequest, domainException.Message, null),
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message, null),
            ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message, null),
            UnauthorizedException unauthorized => (StatusCodes.Status401Unauthorized, unauthorized.Message, null),
            ForbiddenException forbidden => (StatusCodes.Status403Forbidden, forbidden.Message, null),

            _ => (StatusCodes.Status500InternalServerError,
                  "An unexpected error occurred while processing your request.", null),
        };
}
