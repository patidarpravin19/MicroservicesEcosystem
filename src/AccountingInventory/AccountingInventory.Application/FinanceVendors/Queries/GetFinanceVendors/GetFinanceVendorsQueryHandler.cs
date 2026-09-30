using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.FinanceVendors.Queries.GetFinanceVendors;

public sealed class GetFinanceVendorsQueryHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetFinanceVendorsQuery, PagedResult<FinanceVendorSummary>>
{
    public async Task<PagedResult<FinanceVendorSummary>> Handle(GetFinanceVendorsQuery request, CancellationToken cancellationToken)
    {
        var query = db.FinanceVendors.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search) || x.Code.ToLower().Contains(search) ||
                x.Mobile.ToLower().Contains(search) || x.Email.ToLower().Contains(search) ||
                x.ContactName.ToLower().Contains(search) || x.ContactMobile.ToLower().Contains(search) ||
                (x.Description != null && x.Description.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var descending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        var items = await ApplySort(query, request.SortBy, descending)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(x => new FinanceVendorSummary(x.Id, x.Name, x.Code, x.Mobile, x.Email,
                x.ContactName, x.ContactMobile, x.Description, x.IsActive))
            .ToListAsync(cancellationToken);
        return new(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    private static IOrderedQueryable<FinanceVendor> ApplySort(
        IQueryable<FinanceVendor> query, string? sortBy, bool descending)
        => (sortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("code", false) => query.OrderBy(x => x.Code), ("code", true) => query.OrderByDescending(x => x.Code),
            ("mobile", false) => query.OrderBy(x => x.Mobile), ("mobile", true) => query.OrderByDescending(x => x.Mobile),
            ("email", false) => query.OrderBy(x => x.Email), ("email", true) => query.OrderByDescending(x => x.Email),
            ("contactname", false) => query.OrderBy(x => x.ContactName), ("contactname", true) => query.OrderByDescending(x => x.ContactName),
            ("contactmobile", false) => query.OrderBy(x => x.ContactMobile), ("contactmobile", true) => query.OrderByDescending(x => x.ContactMobile),
            ("description", false) => query.OrderBy(x => x.Description), ("description", true) => query.OrderByDescending(x => x.Description),
            (_, true) => query.OrderByDescending(x => x.Name), _ => query.OrderBy(x => x.Name)
        };
}
