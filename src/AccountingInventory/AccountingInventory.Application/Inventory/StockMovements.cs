using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using AccountingInventory.Application.GeneralLedger;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Application.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Inventory;

public sealed record WriteOffInventoryCommand(Guid ProductId, DateOnly AdjustmentDate, string Reason, Guid? ApprovalId = null) : IRequest<Guid>;

public sealed class WriteOffInventoryValidator : AbstractValidator<WriteOffInventoryCommand>
{
    public WriteOffInventoryValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.AdjustmentDate).NotEmpty();
        RuleFor(command => command.Reason).NotEmpty().MaximumLength(300);
    }
}

public sealed class WriteOffInventoryHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<WriteOffInventoryCommand, Guid>
{
    public async Task<Guid> Handle(WriteOffInventoryCommand request, CancellationToken ct)
    {
        await ApprovalGate.ValidateAsync(db, request.ApprovalId, ApprovalAction.InventoryWriteOff, request.ProductId.ToString(),
            new { productId = request.ProductId, adjustmentDate = request.AdjustmentDate, reason = request.Reason }, ct);
        var product = await db.Products.SingleOrDefaultAsync(item => item.Id == request.ProductId, ct)
            ?? throw new NotFoundException("Product was not found.");
        if (!product.IsActive || product.IsSold)
            throw new ConflictException("Only active, unsold inventory can be written off.");
        var cost = decimal.Round(product.PurchasePrice - product.Discount, 2, MidpointRounding.AwayFromZero);
        var adjustment = InventoryAdjustment.WriteOff(product.Id, request.AdjustmentDate, request.Reason, cost);
        var accounts = await LedgerPosting.EnsureSystemAccountsAsync(db, ct);
        product.WriteOff();
        db.InventoryAdjustments.Add(adjustment);
        LedgerPosting.Add(db, LedgerPosting.ForInventoryWriteOff(adjustment, accounts));
        await LedgerPosting.EnsurePeriodOpenAsync(db, adjustment.AdjustmentDate, ct);
        await db.SaveChangesAsync(ct);
        return adjustment.Id;
    }
}

public sealed record StockMovementSummary(DateOnly MovementDate, string MovementType, Guid ProductId,
    string SerialNumber, string? Reference, decimal Quantity, decimal InventoryValueChange, string? Note);
public sealed record GetStockMovementsQuery(int Page = 1, int PageSize = 20, string? Search = null,
    DateOnly? FromDate = null, DateOnly? ToDate = null) : IRequest<PagedResult<StockMovementSummary>>;

public sealed class GetStockMovementsHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetStockMovementsQuery, PagedResult<StockMovementSummary>>
{
    public async Task<PagedResult<StockMovementSummary>> Handle(GetStockMovementsQuery request, CancellationToken ct)
    {
        var products = await db.Products.AsNoTracking().Select(product => new
        {
            product.Id, product.SerialNumber, product.BillNumber, product.PurchaseDate,
            Cost = product.PurchasePrice - product.Discount
        }).ToListAsync(ct);
        var productMap = products.ToDictionary(product => product.Id);
        var movements = products.Select(product => new StockMovementSummary(product.PurchaseDate, "Purchase",
            product.Id, product.SerialNumber, product.BillNumber, 1, product.Cost, null)).ToList();

        var sales = await db.SalesProducts.AsNoTracking().Select(sale => new
        {
            sale.Id, sale.ProductId, sale.BillNumber, sale.SaleDate
        }).ToListAsync(ct);
        foreach (var sale in sales)
            if (Guid.TryParse(sale.ProductId, out var productId) && productMap.TryGetValue(productId, out var product))
                movements.Add(new StockMovementSummary(sale.SaleDate, "Sale", product.Id, product.SerialNumber,
                    sale.BillNumber, -1, -product.Cost, null));

        var adjustments = await db.InventoryAdjustments.AsNoTracking().Select(adjustment => new
        {
            adjustment.Id, adjustment.ProductId, adjustment.AdjustmentDate, adjustment.Reason, adjustment.Cost
        }).ToListAsync(ct);
        foreach (var adjustment in adjustments)
            if (productMap.TryGetValue(adjustment.ProductId, out var product))
                movements.Add(new StockMovementSummary(adjustment.AdjustmentDate, "Write-off", product.Id,
                    product.SerialNumber, adjustment.Id.ToString(), -1, -adjustment.Cost, adjustment.Reason));

        if (request.FromDate.HasValue) movements = movements.Where(row => row.MovementDate >= request.FromDate.Value).ToList();
        if (request.ToDate.HasValue) movements = movements.Where(row => row.MovementDate <= request.ToDate.Value).ToList();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            movements = movements.Where(row => row.SerialNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (row.Reference?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (row.Note?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 100);
        var ordered = movements.OrderByDescending(row => row.MovementDate).ThenBy(row => row.SerialNumber).ToArray();
        return new PagedResult<StockMovementSummary>(ordered.Skip((page - 1) * size).Take(size).ToArray(), page, size,
            ordered.Length, ordered.Length == 0 ? 0 : (int)Math.Ceiling(ordered.Length / (double)size));
    }
}
