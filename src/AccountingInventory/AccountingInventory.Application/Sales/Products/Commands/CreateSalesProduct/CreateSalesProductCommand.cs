using MediatR;

namespace AccountingInventory.Application.Sales.Products.Commands.CreateSalesProduct;

public sealed record CreateSalesProductCommand(
    string ProductId,
    string CustomerName,
    string CustomerMobile,
    string CustomerAddress,
    string? CustomerEmail,
    DateOnly SaleDate,
    decimal ProductPrice,
    decimal SellingPrice,
    decimal Discount) : IRequest<SalesProductSummary>;
