using MediatR;

namespace AccountingInventory.Application.Products.Commands.UpdateProduct;

public sealed record UpdateProductCommand(
    Guid Id, Guid VendorId, Guid BrandId, Guid ProductTypeId, Guid ProductModelId, Guid VariantId, Guid ColorId,
    string SerialNumber, string? SerialNumber1, int Quantity, decimal PurchasePrice, decimal Discount,
    decimal Cgst, decimal Sgst, decimal Tax) : IRequest<UpdateProductResult>;

public sealed record UpdateProductResult(
    Guid Id, Guid VendorId, Guid BrandId, Guid ProductTypeId, Guid ProductModelId, Guid VariantId,
    Guid ColorId, string SerialNumber, string? SerialNumber1, int Quantity, decimal PurchasePrice,
    decimal Discount, decimal Cgst, decimal Sgst, decimal Tax, bool IsActive);
