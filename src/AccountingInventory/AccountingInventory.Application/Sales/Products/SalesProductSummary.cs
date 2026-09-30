namespace AccountingInventory.Application.Sales.Products;

public sealed record SalesProductSummary(
    Guid Id,
    string ProductId,
    string ProductName,
    string SerialNumber,
    string CustomerName,
    string CustomerMobile,
    string CustomerAddress,
    DateOnly SaleDate,
    decimal ProductPrice,
    decimal SellingPrice,
    decimal Discount,
    bool IsActive);
