using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Sales.Products.Queries.GetSalesProducts;

public sealed class GetSalesProductsQueryHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetSalesProductsQuery, PagedResult<SalesProductSummary>>
{
    public async Task<PagedResult<SalesProductSummary>> Handle(GetSalesProductsQuery request, CancellationToken cancellationToken)
    {
        var query = db.SalesProducts.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.ProductId.ToLower().Contains(search)
                || x.CustomerName.ToLower().Contains(search) ||
                x.CustomerMobile.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var items = await query.OrderByDescending(x => x.SaleDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new SalesProductSummary(x.Id, x.ProductId,
                x.CustomerName, x.CustomerMobile, x.CustomerAddress, x.SaleDate, x.ProductPrice,
                x.SellingPrice, x.Discount, x.IsActive))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
