using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Purchases.Products.Commands.CreateProduct;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Purchases.Products.Commands.BulkCreateProducts;

public sealed class BulkCreateProductsCommandHandler(IAccountingInventoryDbContext dbContext, ILogger<BulkCreateProductsCommandHandler> logger)
    : IRequestHandler<BulkCreateProductsCommand, IReadOnlyList<CreateProductResult>>
{
    public async Task<IReadOnlyList<CreateProductResult>> Handle(
        BulkCreateProductsCommand request, CancellationToken cancellationToken)
    {
        var products = request.Products;
        var serials = products.SelectMany(x => new[] { x.SerialNumber.Trim(), x.SerialNumber1!.Trim() }).ToArray();
        var duplicate = serials.GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicate is not null)
            throw new ConflictException($"Serial number '{duplicate}' appears more than once in this purchase.");

        var existing = await dbContext.Products
            .Where(x => serials.Contains(x.SerialNumber) || (x.SerialNumber1 != null && serials.Contains(x.SerialNumber1)))
            .Select(x => x.SerialNumber)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
            throw new ConflictException($"A product with serial number '{existing}' already exists.");

        var entities = products.Select(item => Product.Create(item.VendorId, item.BrandId,
            item.ProductTypeId, item.ProductModelId, item.VariantId, item.ColorId,
            item.SerialNumber, item.SerialNumber1, item.Quantity, item.PurchasePrice,
            item.Discount, item.Cgst, item.Sgst, item.Tax)).ToArray();
        dbContext.Products.AddRange(entities);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Product {ProductId} ({SerialNumber}) added successfully.",
        string.Join(", ", request.Products.Select(p => p.SerialNumber)));

        return entities.Select(product => new CreateProductResult(product.Id, product.VendorId,
            product.BrandId, product.ProductTypeId, product.ProductModelId, product.VariantId,
            product.ColorId, product.SerialNumber, product.SerialNumber1, product.Quantity,
            product.PurchasePrice, product.TotalAmount, product.Discount, product.Cgst,
            product.Sgst, product.Tax, product.IsActive)).ToArray();
    }
}
