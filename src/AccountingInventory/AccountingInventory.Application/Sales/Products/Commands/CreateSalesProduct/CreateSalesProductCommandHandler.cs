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
        if (!Guid.TryParse(request.ProductId, out var productId))
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");

        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        if (!product.IsActive || product.IsSold)
            throw new ConflictException("This product has already been sold or is inactive.");
        if (await db.SalesProducts.AnyAsync(x => x.ProductId == productId.ToString(), cancellationToken))
            throw new ConflictException($"A sales product with product ID '{request.ProductId}' already exists.");

        var customer = await CustomerResolver.GetOrCreateAsync(db, request.CustomerName,
            request.CustomerMobile, request.CustomerAddress, request.CustomerEmail, cancellationToken);
        var billNumber = await db.GenerateSalesBillNumberAsync(DateTime.UtcNow.Year, cancellationToken);
        product.MarkSold();
        var sale = SalesProduct.Create(billNumber, productId.ToString(),
            customer.Id, request.SaleDate,
            request.ProductPrice, request.SellingPrice, request.Discount);
        db.SalesProducts.Add(sale);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Stock changed while the sale was being saved. Refresh and try again.");
        }
        return (await SalesProductSummaryMapper.MapAsync(db, [sale], cancellationToken))[0];
    }
}
