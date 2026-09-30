using AccountingInventory.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Sales.Products.Commands.DeleteSalesProduct;

public sealed class DeleteSalesProductCommandHandler(IAccountingInventoryDbContext db) : IRequestHandler<DeleteSalesProductCommand>
{
    public async Task Handle(DeleteSalesProductCommand request, CancellationToken cancellationToken)
    {
        var sale = await db.SalesProducts.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Sales product '{request.Id}' was not found.");
        sale.Delete();
        await db.SaveChangesAsync(cancellationToken);
    }
}
