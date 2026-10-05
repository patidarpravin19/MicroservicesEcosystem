using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Sales.Products.Commands.CreateSalesProduct;

public sealed class CreateSalesProductCommandHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<CreateSalesProductCommand, SalesProductSummary>
{
    public async Task<SalesProductSummary> Handle(CreateSalesProductCommand request, CancellationToken cancellationToken)
    {
        if (await db.SalesProducts.AnyAsync(x => x.ProductId == request.ProductId.Trim(), cancellationToken))
            throw new ConflictException($"A sales product with product ID '{request.ProductId}' already exists.");

        var sale = SalesProduct.Create(request.ProductId,
            request.CustomerName, request.CustomerMobile, request.CustomerAddress, request.SaleDate,
            request.ProductPrice, request.SellingPrice, request.Discount);
        db.SalesProducts.Add(sale);
        await db.SaveChangesAsync(cancellationToken);
        return (await SalesProductSummaryMapper.MapAsync(db, [sale], cancellationToken))[0];
    }
}
