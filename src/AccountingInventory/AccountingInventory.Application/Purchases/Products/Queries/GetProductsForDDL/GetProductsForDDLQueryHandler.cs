using AccountingInventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Purchases.Products.Queries.GetProductsForDDL;

public sealed class GetProductsForDDLQueryHandler(IAccountingInventoryDbContext dbContext)
    : IRequestHandler<GetProductsForDDLQuery, IEnumerable<GetProductsForDDLSummary>>
{
    public async Task<IEnumerable<GetProductsForDDLSummary>> Handle(
        GetProductsForDDLQuery request, CancellationToken cancellationToken)
        => await dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.SerialNumber)
            .Select(product => new GetProductsForDDLSummary(
                product.Id,
                dbContext.Brands
                    .Where(brand => brand.Id == product.BrandId)
                    .Select(brand => brand.Name)
                    .FirstOrDefault() ?? string.Empty,
                dbContext.ProductTypes
                    .Where(type => type.Id == product.ProductTypeId)
                    .Select(type => type.Name)
                    .FirstOrDefault() ?? string.Empty,
                dbContext.ProductModels
                    .Where(model => model.Id == product.ProductModelId)
                    .Select(model => model.Name)
                    .FirstOrDefault() ?? string.Empty,
                dbContext.Variants
                    .Where(variant => variant.Id == product.VariantId)
                    .Select(variant => variant.Name)
                    .FirstOrDefault() ?? string.Empty,
                dbContext.Colors
                    .Where(color => color.Id == product.ColorId)
                    .Select(color => color.Name)
                    .FirstOrDefault() ?? string.Empty,
                product.SerialNumber,
                product.SerialNumber1!,
                product.PurchasePrice,
                product.Discount))
            .ToListAsync(cancellationToken);
}