namespace AccountingInventory.Application.Sales.Products;

public sealed record SalesProductSummary(
    Guid Id,
    string ProductId,
    string ProductName,
    string SerialNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerMobile,
    string CustomerAddress,
    string? CustomerEmail,
    DateOnly SaleDate,
    decimal ProductPrice,
    decimal SellingPrice,
    decimal Discount,
    bool IsActive,
    string? PaymentMode,
    string BillNumber);
