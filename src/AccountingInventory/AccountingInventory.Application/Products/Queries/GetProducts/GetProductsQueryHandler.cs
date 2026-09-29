using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Products.Queries.GetProducts;

public sealed class GetProductsQueryHandler(IAccountingInventoryDbContext dbContext)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductSummary>>
{
    public async Task<PagedResult<ProductSummary>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.SerialNumber.ToLower().Contains(search) ||
                (x.SerialNumber1 != null && x.SerialNumber1.ToLower().Contains(search)));
        }
        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderBy(x => x.SerialNumber).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new ProductSummary(x.Id, x.VendorId, x.BrandId, x.ProductTypeId, x.ProductModelId,
                x.VariantId, x.ColorId, x.SerialNumber, x.SerialNumber1, x.Quantity, x.PurchasePrice,
                x.Discount, x.Cgst, x.Sgst, x.Tax, x.IsActive)).ToListAsync(cancellationToken);
        return new PagedResult<ProductSummary>(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
