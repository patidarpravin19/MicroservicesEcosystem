using AccountingInventory.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Purchases.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandHandler(
    IAccountingInventoryDbContext dbContext,
    ILogger<UpdateProductCommandHandler> logger) : IRequestHandler<UpdateProductCommand, UpdateProductResult>
{
    public async Task<UpdateProductResult> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"A Product with ID '{request.Id}' was not found.");
        if (product.BillNumber is { } paidBill && await dbContext.PurchasePayments.AnyAsync(payment =>
                payment.VendorId == product.VendorId && payment.BillNumber.ToLower() == paidBill.ToLower(), cancellationToken))
            throw new ConflictException("Products on a bill with recorded payments cannot be edited. Correct the bill through an accounting adjustment.");
        var serialNumber = request.SerialNumber.Trim();
        var serialNumber1 = request.SerialNumber1!.Trim();
        if (string.Equals(serialNumber, serialNumber1, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException("Serial Number and Serial Number 1 must be different.");
        if (await dbContext.Products.AnyAsync(x => x.Id != request.Id
            && (x.SerialNumber == serialNumber || x.SerialNumber == serialNumber1
                || x.SerialNumber1 == serialNumber || x.SerialNumber1 == serialNumber1), cancellationToken))
            throw new ConflictException("A serial number already exists on another product.");

        product.Update(request.VendorId, request.BrandId, request.ProductTypeId, request.ProductModelId,
            request.VariantId, request.ColorId, request.SerialNumber, request.SerialNumber1, request.BillNumber,
            request.PurchasePrice, request.Discount, request.Cgst, request.Sgst, request.Tax);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Product {ProductId} ({SerialNumber}) updated successfully.", product.Id, product.SerialNumber);
        return new UpdateProductResult(product.Id, product.VendorId, product.BrandId, product.ProductTypeId,
            product.ProductModelId, product.VariantId, product.ColorId, product.SerialNumber, product.SerialNumber1,
            product.PurchasePrice, product.TotalAmount, product.Discount, product.Cgst, product.Sgst,
            product.Tax, product.IsActive, product.IsSold, product.BillNumber);
    }
}
