using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.ProductModels.Queries.GetProductModels;

public sealed class GetProductModelsQueryHandler(IAccountingInventoryDbContext accountingInventoryDbContext)
    : IRequestHandler<GetProductModelsQuery, PagedResult<ProductModelSummary>>
{
    public async Task<PagedResult<ProductModelSummary>> Handle(GetProductModelsQuery request, CancellationToken cancellationToken)
    {
        var query = accountingInventoryDbContext.ProductModels.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(search) ||
                (v.Description != null && v.Description.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var items = await ApplySort(query, request.SortBy, descending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProductModel => new ProductModelSummary(
                ProductModel.Id,
                brandName : 
                accountingInventoryDbContext.Brands
                    .Where(brand => brand.Id == ProductModel.BrandId)
                    .Select(brand => brand.Name)
                    .FirstOrDefault() ?? string.Empty,
                
                productTypeName : 
                accountingInventoryDbContext.ProductTypes
                    .Where(productType => productType.Id == ProductModel.ProductTypeId)
                    .Select(productType => productType.Name)
                    .FirstOrDefault() ?? string.Empty,

                ProductModel.Name,
                ProductModel.Description ?? string.Empty,
                ProductModel.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductModelSummary>(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    private static IOrderedQueryable<ProductModel> ApplySort(IQueryable<ProductModel> query, string? sortBy, bool descending)
        => (sortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("name", false) => query.OrderBy(v => v.Name),
            ("name", true) => query.OrderByDescending(v => v.Name),
            ("code", false) => query.OrderBy(v => v.Code),
            ("code", true) => query.OrderByDescending(v => v.Code),
            ("description", false) => query.OrderBy(v => v.Description),
            ("description", true) => query.OrderByDescending(v => v.Description),
            (_, true) => query.OrderByDescending(v => v.Name),
            _ => query.OrderBy(v => v.Name)
        };
}


