using MediatR;

namespace AccountingInventory.Application.Products.Queries.GetProductsForDDL;

public sealed record GetProductsForDDLQuery : IRequest<IEnumerable<GetProductsForDDLSummary>>;

public sealed record GetProductsForDDLSummary(Guid Id, string ProductName, string SerialNumber, decimal ProductPrice);
