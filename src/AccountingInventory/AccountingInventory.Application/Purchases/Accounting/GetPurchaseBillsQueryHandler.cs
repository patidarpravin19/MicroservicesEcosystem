using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Purchases.Accounting;

public sealed record GetPurchaseBillsQuery(int Page = 1, int PageSize = 20, string? Search = null, string? SortBy = null, string? SortDirection = null)
    : IRequest<PagedResult<PurchaseBillSummary>>;

public sealed record PurchaseBillSummary(
    string Id, Guid VendorId, string VendorName, string BillNumber, DateOnly BillDate, int PaymentTermsDays, DateOnly DueDate,
    decimal TotalAmount, decimal AmountPaid, decimal Balance, string PaymentStatus);

public sealed record PurchasePaymentSummary(
    Guid Id, decimal Amount, string PaymentMode, DateOnly PaymentDate,
    string? ReferenceNumber, string? Note);

public sealed record PurchaseBillDetails(PurchaseBillSummary Bill, IReadOnlyList<PurchasePaymentSummary> Payments);

public sealed record GetPurchaseBillDetailsQuery(Guid VendorId, string BillNumber)
    : IRequest<PurchaseBillDetails>;

public sealed class GetPurchaseBillsQueryHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetPurchaseBillsQuery, PagedResult<PurchaseBillSummary>>
{
    public async Task<PagedResult<PurchaseBillSummary>> Handle(GetPurchaseBillsQuery request, CancellationToken cancellationToken)
    {
        var products = db.Products.AsNoTracking().Where(product => product.BillNumber != null);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            products = products.Where(product => product.BillNumber!.ToLower().Contains(search)
                || db.Vendors.Any(vendor => vendor.Id == product.VendorId && vendor.Name.ToLower().Contains(search)));
        }

        var invoices = products.GroupBy(product => new { product.VendorId, product.BillNumber })
            .Select(group => new
            {
                group.Key.VendorId,
                BillNumber = group.Key.BillNumber!,
                BillDate = group.Min(product => product.PurchaseDate),
                PaymentTermsDays = group.Max(product => product.PaymentTermsDays),
                DueDate = group.Max(product => product.DueDate),
                TotalAmount = group.Sum(product => product.TotalAmount)
            });

        var totalCount = await invoices.CountAsync(cancellationToken);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var rows = await (from invoice in invoices
                          join vendor in db.Vendors.AsNoTracking() on invoice.VendorId equals vendor.Id
                          orderby invoice.BillDate descending, invoice.BillNumber
                          select new { invoice.VendorId, VendorName = vendor.Name, invoice.BillNumber, invoice.BillDate, invoice.PaymentTermsDays, invoice.DueDate, invoice.TotalAmount })
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync(cancellationToken);

        var vendorIds = rows.Select(row => row.VendorId).Distinct().ToArray();
        var billNumbers = rows.Select(row => row.BillNumber.ToLower()).Distinct().ToArray();
        var paidAmounts = await db.PurchasePayments.AsNoTracking()
            .Where(payment => vendorIds.Contains(payment.VendorId)
                && billNumbers.Contains(payment.BillNumber.ToLower()))
            .GroupBy(payment => new { payment.VendorId, BillNumber = payment.BillNumber.ToLower() })
            .Select(group => new { group.Key.VendorId, group.Key.BillNumber, AmountPaid = group.Sum(payment => payment.Amount) })
            .ToListAsync(cancellationToken);
        var paidByInvoice = paidAmounts.ToDictionary(
            payment => (payment.VendorId, payment.BillNumber), payment => payment.AmountPaid);

        var items = rows.Select(row =>
        {
            var amountPaid = paidByInvoice.GetValueOrDefault((row.VendorId, row.BillNumber.ToLower()));
            var balance = Math.Max(0m, row.TotalAmount - amountPaid);
            var status = balance == 0m ? "Paid" : amountPaid > 0m ? "Partially paid" : "Unpaid";
            return new PurchaseBillSummary($"{row.VendorId}:{row.BillNumber}", row.VendorId, row.VendorName, row.BillNumber,
                row.BillDate, row.PaymentTermsDays, row.DueDate, row.TotalAmount, amountPaid, balance, status);
        }).ToArray();

        return new PagedResult<PurchaseBillSummary>(items, page, pageSize, totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}

public sealed class GetPurchaseBillDetailsQueryHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetPurchaseBillDetailsQuery, PurchaseBillDetails>
{
    public async Task<PurchaseBillDetails> Handle(GetPurchaseBillDetailsQuery request, CancellationToken cancellationToken)
    {
        var normalizedBillNumber = request.BillNumber.Trim().ToLower();
        var invoice = await db.Products.AsNoTracking()
            .Where(product => product.VendorId == request.VendorId && product.BillNumber != null
                && product.BillNumber.ToLower() == normalizedBillNumber)
            .GroupBy(product => new { product.VendorId, product.BillNumber })
            .Select(group => new
            {
                group.Key.VendorId,
                BillNumber = group.Key.BillNumber!,
                BillDate = group.Min(product => product.PurchaseDate),
                PaymentTermsDays = group.Max(product => product.PaymentTermsDays),
                DueDate = group.Max(product => product.DueDate),
                TotalAmount = group.Sum(product => product.TotalAmount)
            }).FirstOrDefaultAsync(cancellationToken);

        if (invoice is null)
            throw new BuildingBlocks.Application.Exceptions.NotFoundException(
                $"Purchase bill '{request.BillNumber}' was not found for vendor '{request.VendorId}'.");

        var payments = await db.PurchasePayments.AsNoTracking()
            .Where(payment => payment.VendorId == request.VendorId
                && payment.BillNumber.ToLower() == normalizedBillNumber)
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.CreatedAt)
            .Select(payment => new PurchasePaymentSummary(payment.Id, payment.Amount, payment.PaymentMode,
                payment.PaymentDate, payment.ReferenceNumber, payment.Note))
            .ToListAsync(cancellationToken);
        var amountPaid = payments.Sum(payment => payment.Amount);
        var balance = Math.Max(0m, invoice.TotalAmount - amountPaid);
        var status = balance == 0m ? "Paid" : amountPaid > 0m ? "Partially paid" : "Unpaid";
        var vendorName = await db.Vendors.AsNoTracking().Where(vendor => vendor.Id == invoice.VendorId)
            .Select(vendor => vendor.Name).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var bill = new PurchaseBillSummary($"{invoice.VendorId}:{invoice.BillNumber}", invoice.VendorId, vendorName, invoice.BillNumber,
            invoice.BillDate, invoice.PaymentTermsDays, invoice.DueDate, invoice.TotalAmount, amountPaid, balance, status);
        return new PurchaseBillDetails(bill, payments);
    }
}
