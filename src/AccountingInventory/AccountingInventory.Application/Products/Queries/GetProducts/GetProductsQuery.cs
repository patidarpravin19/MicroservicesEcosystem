using AccountingInventory.Application.Common.Models;
using MediatR;

namespace AccountingInventory.Application.Products.Queries.GetProducts;

public sealed record GetProductsQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedResult<ProductSummary>>;

public sealed record ProductSummary(Guid Id, Guid VendorId, Guid BrandId, Guid ProductTypeId,
    Guid ProductModelId, Guid VariantId, Guid ColorId, string SerialNumber, string? SerialNumber1,
    int Quantity, decimal PurchasePrice, decimal Discount, decimal Cgst, decimal Sgst, decimal Tax,
    bool IsActive)
{
    internal static ProductSummary From(AccountingInventory.Domain.Entities.Product x) => new(x.Id, x.VendorId,
        x.BrandId, x.ProductTypeId, x.ProductModelId, x.VariantId, x.ColorId, x.SerialNumber, x.SerialNumber1,
        x.Quantity, x.PurchasePrice, x.Discount, x.Cgst, x.Sgst, x.Tax, x.IsActive);
}
