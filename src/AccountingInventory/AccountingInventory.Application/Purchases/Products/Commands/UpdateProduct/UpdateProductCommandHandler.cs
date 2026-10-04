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
        if (await dbContext.Products.AnyAsync(x => x.Id != request.Id && x.SerialNumber == request.SerialNumber.Trim(), cancellationToken))
            throw new ConflictException($"A Product with serial number '{request.SerialNumber}' already exists.");

        product.Update(request.VendorId, request.BrandId, request.ProductTypeId, request.ProductModelId,
            request.VariantId, request.ColorId, request.SerialNumber, request.SerialNumber1, request.Quantity,
            request.PurchasePrice, request.Discount, request.Cgst, request.Sgst, request.Tax);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Product {ProductId} ({SerialNumber}) updated successfully.", product.Id, product.SerialNumber);
        return new UpdateProductResult(product.Id, product.VendorId, product.BrandId, product.ProductTypeId,
            product.ProductModelId, product.VariantId, product.ColorId, product.SerialNumber, product.SerialNumber1,
            product.Quantity, product.PurchasePrice, product.Discount, product.Cgst, product.Sgst, product.Tax, product.IsActive);
    }
}
