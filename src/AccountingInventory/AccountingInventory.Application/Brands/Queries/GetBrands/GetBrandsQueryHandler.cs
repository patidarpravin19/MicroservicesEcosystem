using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Brands.Queries.GetBrands;

public sealed class GetBrandsQueryHandler(IAccountingInventoryDbContext accountingInventoryDbContext)
    : IRequestHandler<GetBrandsQuery, PagedResult<BrandSummary>>
{
    public async Task<PagedResult<BrandSummary>> Handle(GetBrandsQuery request, CancellationToken cancellationToken)
    {
        var query = accountingInventoryDbContext.Brands.AsNoTracking();
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
            .Select(brand => new BrandSummary(
                brand.Id,
                accountingInventoryDbContext.Vendors
                    .Where(vendor => vendor.Id == brand.VendorId)
                    .Select(vendor => vendor.Name)
                    .FirstOrDefault() ?? string.Empty,
                brand.Name,
                brand.Description ?? string.Empty,
                brand.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<BrandSummary>(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    private static IOrderedQueryable<Brand> ApplySort(IQueryable<Brand> query, string? sortBy, bool descending)
        => (sortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("name", false) => query.OrderBy(v => v.Name),
            ("name", true) => query.OrderByDescending(v => v.Name),
            ("description", false) => query.OrderBy(v => v.Description),
            ("description", true) => query.OrderByDescending(v => v.Description),
            (_, true) => query.OrderByDescending(v => v.Name),
            _ => query.OrderBy(v => v.Name)
        };
}
