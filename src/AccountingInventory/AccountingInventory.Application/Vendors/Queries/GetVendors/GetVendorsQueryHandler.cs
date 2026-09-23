using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Vendors.Queries.GetVendors;

public sealed class GetVendorsQueryHandler(IAccountingInventoryDbContext accountingInventoryDbContext)
    : IRequestHandler<GetVendorsQuery, PagedVendors>
{
    public async Task<PagedVendors> Handle(GetVendorsQuery request, CancellationToken cancellationToken)
    {
        var query = accountingInventoryDbContext.Vendors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(v => v.Name.ToLower().Contains(search) ||
                v.Code.ToLower().Contains(search) || v.Mobile.ToLower().Contains(search) ||
                v.Email.ToLower().Contains(search) ||
                (v.Description != null && v.Description.ToLower().Contains(search)) ||
                (v.Address != null && v.Address.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var items = await ApplySort(query, request.SortBy, descending)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VendorSummary(v.Id, v.Name, v.Code, v.Mobile, v.Email, v.Description!, v.Address!, v.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedVendors(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    private static IOrderedQueryable<Vendor> ApplySort(IQueryable<Vendor> query, string? sortBy, bool descending)
        => (sortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("code", false) => query.OrderBy(v => v.Code),
            ("code", true) => query.OrderByDescending(v => v.Code),
            ("mobile", false) => query.OrderBy(v => v.Mobile),
            ("mobile", true) => query.OrderByDescending(v => v.Mobile),
            ("email", false) => query.OrderBy(v => v.Email),
            ("email", true) => query.OrderByDescending(v => v.Email),
            ("description", false) => query.OrderBy(v => v.Description),
            ("description", true) => query.OrderByDescending(v => v.Description),
            ("address", false) => query.OrderBy(v => v.Address),
            ("address", true) => query.OrderByDescending(v => v.Address),
            (_, true) => query.OrderByDescending(v => v.Name),
            _ => query.OrderBy(v => v.Name)
        };
}
