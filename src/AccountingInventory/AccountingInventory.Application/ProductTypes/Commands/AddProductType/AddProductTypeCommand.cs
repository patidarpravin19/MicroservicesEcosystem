using MediatR;

namespace AccountingInventory.Application.ProductTypes.Commands.AddProductType;

/// <summary>Self-service ProductType add — deliberately anonymous at the API layer
/// this in a production deployment.</summary>
public sealed record AddProductTypeCommand(Guid BrandId, string Name, string Description) : IRequest<AddProductTypeResult>;

public sealed record AddProductTypeResult(Guid Id, Guid BrandId, string Name, string Description);

