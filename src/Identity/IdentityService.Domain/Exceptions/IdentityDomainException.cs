namespace IdentityService.Domain.Exceptions;

/// <summary>
/// Thrown when an invariant of the Identity domain model is violated. Application-layer
/// exception handling never needs to know about this type directly — the API's global
/// exception handler maps it to a 400 ProblemDetails response by convention (see
/// GlobalExceptionHandler in IdentityService.Api).
/// </summary>
public sealed class IdentityDomainException(string message) : Exception(message);
