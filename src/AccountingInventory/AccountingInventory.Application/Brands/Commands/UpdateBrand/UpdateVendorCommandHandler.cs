using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountingInventory.Application.Vendors.Commands.UpdateVendor;

/// <summary>
/// The entire vendor add flow, in one handler, entirely inside AccountingInventory:
/// </summary>
public sealed class UpdateBrandCommandHandler(
    IAccountingInventoryDbContext accountingInventoryDbContext,
    ILogger<UpdateBrandCommandHandler> logger)
    : IRequestHandler<UpdateBrandCommand, UpdateBrandResult>
{

    public async Task<UpdateBrandResult> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Name.Trim().ToLowerInvariant();

        var brandTaken = await accountingInventoryDbContext.Brands.AnyAsync(t => t.Id == request.Id, cancellationToken);

        if (!brandTaken)
        {
            logger.LogWarning("Brand not found: ID {Id}.", request.Id);
            throw new NotFoundException($"A brand with ID '{request.Id}' was not found.");
        }

        var brand = Brand.Update(request.Id, request.VendorId, request.Name, request.Description);

        accountingInventoryDbContext.Brands.Update(brand);
        await accountingInventoryDbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Brand {BrandId} - ({Name}) - {Description} update successfully.",
            brand.Id, brand.Name, brand.Description);

        return new UpdateBrandResult(brand.Id, brand.VendorId, brand.Name, brand.Description!);
    }
}
