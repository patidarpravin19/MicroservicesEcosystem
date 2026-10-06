using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Purchases.Products.Commands.UpdateProduct;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Purchases.Products.Commands.BulkUpdateProducts;

public sealed class BulkUpdateProductsCommandHandler(IAccountingInventoryDbContext dbContext)
    : IRequestHandler<BulkUpdateProductsCommand, IReadOnlyList<UpdateProductResult>>
{
    public async Task<IReadOnlyList<UpdateProductResult>> Handle(
        BulkUpdateProductsCommand request, CancellationToken cancellationToken)
    {
        var updates = request.Products;
        var ids = updates.Select(update => update.Id).ToArray();
        if (ids.Distinct().Count() != ids.Length)
            throw new ConflictException("The bulk update contains the same product more than once.");

        var serials = updates
            .SelectMany(update => new[] { update.SerialNumber.Trim(), update.SerialNumber1!.Trim() })
            .ToArray();
        var duplicateSerial = serials.GroupBy(serial => serial, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateSerial is not null)
            throw new ConflictException($"Serial number '{duplicateSerial}' appears more than once in the selected products.");

        var products = await dbContext.Products
            .Where(product => ids.Contains(product.Id))
            .ToListAsync(cancellationToken);
        if (products.Count != ids.Length)
            throw new NotFoundException("One or more selected products could not be found. Refresh the product list and try again.");

        var existingSerial = await dbContext.Products
            .Where(product => !ids.Contains(product.Id)
                && (serials.Contains(product.SerialNumber)
                    || (product.SerialNumber1 != null && serials.Contains(product.SerialNumber1))))
            .Select(product => serials.Contains(product.SerialNumber) ? product.SerialNumber : product.SerialNumber1!)
            .FirstOrDefaultAsync(cancellationToken);
        if (existingSerial is not null)
            throw new ConflictException($"Serial number '{existingSerial}' is already assigned to another product.");

        var updatesById = updates.ToDictionary(update => update.Id);
        foreach (var product in products)
        {
            var update = updatesById[product.Id];
            product.Update(update.VendorId, update.BrandId, update.ProductTypeId, update.ProductModelId,
                update.VariantId, update.ColorId, update.SerialNumber, update.SerialNumber1,
                update.BillNumber, update.PurchasePrice, update.Discount, update.Cgst, update.Sgst, update.Tax);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return products.Select(product => new UpdateProductResult(product.Id, product.VendorId,
            product.BrandId, product.ProductTypeId, product.ProductModelId, product.VariantId,
            product.ColorId, product.SerialNumber, product.SerialNumber1,
            product.PurchasePrice, product.TotalAmount, product.Discount, product.Cgst,
            product.Sgst, product.Tax, product.IsActive, product.IsSold, product.BillNumber)).ToArray();
    }
}
