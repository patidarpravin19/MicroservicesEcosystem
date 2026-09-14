using BuildingBlocks.Domain;

namespace IdentityService.Domain.Exceptions;

public sealed class IdentityDomainException(string message) : DomainException(message);
