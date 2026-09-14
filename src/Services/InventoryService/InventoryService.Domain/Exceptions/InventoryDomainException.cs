using BuildingBlocks.Domain;

namespace InventoryService.Domain.Exceptions;

public sealed class InventoryDomainException(string message) : DomainException(message);
