using AccountingInventory.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Products.Queries.GetProductsForDDL;

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
                dbContext.ProductModels
                    .Where(model => model.Id == product.ProductModelId)
                    .Select(model => model.Name)
                    .FirstOrDefault() ?? string.Empty,
                product.SerialNumber,
                product.PurchasePrice))
            .ToListAsync(cancellationToken);
}
