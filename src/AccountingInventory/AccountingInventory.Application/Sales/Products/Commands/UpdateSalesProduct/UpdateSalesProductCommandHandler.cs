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
        if (!Guid.TryParse(request.ProductId, out var requestedProductId))
            throw new NotFoundException($"Product '{request.ProductId}' was not found.");
        var originalProductId = Guid.TryParse(sale.ProductId, out var existingId) ? existingId : Guid.Empty;

        if (requestedProductId != originalProductId)
        {
            var nextProduct = await db.Products.SingleOrDefaultAsync(x => x.Id == requestedProductId, cancellationToken)
                ?? throw new NotFoundException($"Product '{request.ProductId}' was not found.");
            if (!nextProduct.IsActive || nextProduct.IsSold)
                throw new ConflictException("This product has already been sold or is inactive.");
            if (await db.SalesProducts.AnyAsync(x => x.Id != request.Id
                    && x.ProductId == requestedProductId.ToString(), cancellationToken))
                throw new ConflictException($"A sales product with product ID '{request.ProductId}' already exists.");

            nextProduct.MarkSold();
            if (originalProductId != Guid.Empty)
            {
                var previousProduct = await db.Products.SingleOrDefaultAsync(x => x.Id == originalProductId, cancellationToken);
                previousProduct?.MarkAvailable();
            }
        }

        var customer = await CustomerResolver.GetOrCreateAsync(db, request.CustomerName,
            request.CustomerMobile, request.CustomerAddress, request.CustomerEmail, cancellationToken);
        sale.Update(requestedProductId.ToString(), customer.Id, request.SaleDate, request.ProductPrice,
            request.SellingPrice, request.Discount);
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
