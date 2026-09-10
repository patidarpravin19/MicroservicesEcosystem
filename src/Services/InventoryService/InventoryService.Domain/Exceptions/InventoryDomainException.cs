namespace InventoryService.Domain.Exceptions;

/// <summary>
/// Thrown when an invariant of the Inventory domain model is violated (e.g. adding
/// negative stock, or exceeding a defined maximum). Mapped to a 400 ProblemDetails
/// response by InventoryService.Api's GlobalExceptionHandler.
/// </summary>
public sealed class InventoryDomainException(string message) : Exception(message);
