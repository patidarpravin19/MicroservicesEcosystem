using MediatR;

namespace AccountingInventory.Application.Vendors.Queries.GetVendors;

public sealed record GetVendorsQuery : IRequest<IReadOnlyList<VendorSummary>>;

public sealed record VendorSummary(Guid Id, string Name, string Code, string Mobile, string Email, string Description, string Address);

