using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Inventory.Queries.GetStock;

public sealed class GetStockQueryHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetStockQuery, PagedResult<StockItemSummary>>
{
    public async Task<PagedResult<StockItemSummary>> Handle(
        GetStockQuery request,
        CancellationToken cancellationToken)
    {
        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(product => product.SerialNumber.ToLower().Contains(search)
                || (product.SerialNumber1 != null && product.SerialNumber1.ToLower().Contains(search))
                || db.Brands.Any(item => item.Id == product.BrandId && item.Name.ToLower().Contains(search))
                || db.ProductModels.Any(item => item.Id == product.ProductModelId && item.Name.ToLower().Contains(search))
                || db.Variants.Any(item => item.Id == product.VariantId && item.Name.ToLower().Contains(search))
                || db.Colors.Any(item => item.Id == product.ColorId && item.Name.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var products = await query.OrderBy(product => product.SerialNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new
            {
                product.Id,
                product.SerialNumber,
                product.SerialNumber1,
                product.TotalAmount,
                product.IsSold,
                product.IsActive,
                Brand = db.Brands.Where(item => item.Id == product.BrandId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty,
                Model = db.ProductModels.Where(item => item.Id == product.ProductModelId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty,
                Variant = db.Variants.Where(item => item.Id == product.VariantId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty,
                Color = db.Colors.Where(item => item.Id == product.ColorId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        var items = products.Select(product =>
        {
            var isSold = product.IsSold;
            var status = !product.IsActive ? "Inactive"
                : isSold ? "Sold"
                : "In stock";
            var name = string.Join(" - ", new[] { product.Brand, product.Model, product.Variant, product.Color }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
            return new StockItemSummary(product.Id, name, product.SerialNumber, product.SerialNumber1,
                product.TotalAmount, isSold, product.IsActive, status);
        }).ToArray();

        return new PagedResult<StockItemSummary>(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
