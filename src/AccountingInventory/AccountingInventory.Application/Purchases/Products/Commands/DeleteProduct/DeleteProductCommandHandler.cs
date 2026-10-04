using AccountingInventory.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Purchases.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IAccountingInventoryDbContext dbContext,
    ILogger<DeleteProductCommandHandler> logger) : IRequestHandler<DeleteProductCommand>
{
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"A Product with ID '{request.Id}' was not found.");
        product.Delete();
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Product {ProductId} deleted successfully.", product.Id);
    }
}
