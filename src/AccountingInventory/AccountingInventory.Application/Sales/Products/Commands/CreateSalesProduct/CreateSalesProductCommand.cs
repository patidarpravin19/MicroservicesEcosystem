using MediatR;

namespace AccountingInventory.Application.Sales.Products.Commands.CreateSalesProduct;

public sealed record CreateSalesProductCommand(
    string ProductId,
    string ProductName,
    string SerialNumber,
    string CustomerName,
    string CustomerMobile,
    string CustomerAddress,
    DateOnly SaleDate,
    decimal ProductPrice,
    decimal SellingPrice,
    decimal Discount) : IRequest<SalesProductSummary>;
