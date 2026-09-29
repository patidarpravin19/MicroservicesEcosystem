using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Products.Queries.GetProducts;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Products.Queries.GetProductById;

public sealed class GetProductByIdQueryHandler(IAccountingInventoryDbContext dbContext)
    : IRequestHandler<GetProductByIdQuery, ProductSummary>
{
    public async Task<ProductSummary> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"A Product with ID '{request.Id}' was not found.");
        return ProductSummary.From(product);
    }
}
