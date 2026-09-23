using MediatR;

namespace AccountingInventory.Application.Vendors.Queries.GetVendors;

public sealed record GetVendorsQuery(
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null,
    string? Search = null) : IRequest<PagedVendors>;

public sealed record PagedVendors(
    IReadOnlyList<VendorSummary> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record VendorSummary(Guid Id, string Name, string Code, string Mobile, string Email, string Description, string Address, bool IsActive);

