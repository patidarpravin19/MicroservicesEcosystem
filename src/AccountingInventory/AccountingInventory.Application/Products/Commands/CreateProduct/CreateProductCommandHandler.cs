using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Products.Commands.CreateProduct;

public sealed class CreateProductCommandHandler(
    IAccountingInventoryDbContext dbContext,
    ILogger<CreateProductCommandHandler> logger) : IRequestHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (await dbContext.Products.AnyAsync(x => x.SerialNumber == request.SerialNumber.Trim(), cancellationToken))
            throw new ConflictException($"A Product with serial number '{request.SerialNumber}' already exists.");

        var product = Product.Create(request.VendorId, request.BrandId, request.ProductTypeId, request.ProductModelId,
            request.VariantId, request.ColorId, request.SerialNumber, request.SerialNumber1, request.Quantity,
            request.PurchasePrice, request.Discount, request.Cgst, request.Sgst, request.Tax);
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Product {ProductId} ({SerialNumber}) added successfully.", product.Id, product.SerialNumber);
        return new CreateProductResult(product.Id, product.VendorId, product.BrandId, product.ProductTypeId,
            product.ProductModelId, product.VariantId, product.ColorId, product.SerialNumber, product.SerialNumber1,
            product.Quantity, product.PurchasePrice, product.Discount, product.Cgst, product.Sgst, product.Tax, product.IsActive);
    }
}
