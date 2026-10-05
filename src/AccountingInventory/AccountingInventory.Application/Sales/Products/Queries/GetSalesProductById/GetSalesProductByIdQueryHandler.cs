using AccountingInventory.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Sales.Products.Queries.GetSalesProductById;

public sealed class GetSalesProductByIdQueryHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetSalesProductByIdQuery, SalesProductSummary>
{
    public async Task<SalesProductSummary> Handle(GetSalesProductByIdQuery request, CancellationToken cancellationToken)
    {
        var sale = await db.SalesProducts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Sales product '{request.Id}' was not found.");
        return new(sale.Id, sale.ProductId, sale.CustomerName,
            sale.CustomerMobile, sale.CustomerAddress, sale.SaleDate, sale.ProductPrice, sale.SellingPrice,
            sale.Discount, sale.IsActive);
    }
}
