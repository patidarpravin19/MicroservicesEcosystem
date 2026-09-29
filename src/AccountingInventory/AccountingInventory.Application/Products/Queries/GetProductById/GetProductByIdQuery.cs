using AccountingInventory.Application.Products.Queries.GetProducts;
using MediatR;

namespace AccountingInventory.Application.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductSummary>;
