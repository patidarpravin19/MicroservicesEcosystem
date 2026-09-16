using AccountingInventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Vendors.Queries.GetVendors;

public sealed class GetVendorsQueryHandler(IAccountingInventoryDbContext accountingInventoryDbContext)
    : IRequestHandler<GetVendorsQuery, IReadOnlyList<VendorSummary>>
{
    public async Task<IReadOnlyList<VendorSummary>> Handle(GetVendorsQuery request, CancellationToken cancellationToken)
        => await accountingInventoryDbContext.Vendors
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new VendorSummary(t.Id, t.Name, t.Code, t.Mobile, t.Email, t.Description!, t.Address!))
            .ToListAsync(cancellationToken);
}
