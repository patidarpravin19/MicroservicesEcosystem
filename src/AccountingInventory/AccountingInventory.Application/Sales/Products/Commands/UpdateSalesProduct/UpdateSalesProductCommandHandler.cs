using AccountingInventory.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Sales.Products.Commands.UpdateSalesProduct;

public sealed class UpdateSalesProductCommandHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<UpdateSalesProductCommand, SalesProductSummary>
{
    public async Task<SalesProductSummary> Handle(UpdateSalesProductCommand request, CancellationToken cancellationToken)
    {
        var sale = await db.SalesProducts.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Sales product '{request.Id}' was not found.");
        if (await db.SalesProducts.AnyAsync(x => x.Id != request.Id && x.SerialNumber == request.SerialNumber.Trim(), cancellationToken))
            throw new ConflictException($"A sales product with serial number '{request.SerialNumber}' already exists.");

        sale.Update(request.ProductId, request.ProductName, request.SerialNumber, request.CustomerName,
            request.CustomerMobile, request.CustomerAddress, request.SaleDate, request.ProductPrice,
            request.SellingPrice, request.Discount);
        await db.SaveChangesAsync(cancellationToken);
        return new(sale.Id, sale.ProductId, sale.ProductName, sale.SerialNumber, sale.CustomerName,
            sale.CustomerMobile, sale.CustomerAddress, sale.SaleDate, sale.ProductPrice, sale.SellingPrice,
            sale.Discount, sale.IsActive);
    }
}
