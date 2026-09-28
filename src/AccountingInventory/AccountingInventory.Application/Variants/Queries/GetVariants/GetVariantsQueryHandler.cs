using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using AccountingInventory.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Variants.Queries.GetVariants;

public sealed class GetVariantsQueryHandler(IAccountingInventoryDbContext accountingInventoryDbContext)
    : IRequestHandler<GetVariantsQuery, PagedResult<VariantSummary>>
{
    public async Task<PagedResult<VariantSummary>> Handle(GetVariantsQuery request, CancellationToken cancellationToken)
    {
        var query = accountingInventoryDbContext.Variants.AsNoTracking();
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
            .Select(variant => new VariantSummary(
                variant.Id,               
                variant.Name,
                variant.Description ?? string.Empty,
                variant.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<VariantSummary>(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    private static IOrderedQueryable<Variant> ApplySort(IQueryable<Variant> query, string? sortBy, bool descending)
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
