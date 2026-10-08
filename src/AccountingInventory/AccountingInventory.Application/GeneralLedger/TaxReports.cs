using AccountingInventory.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.GeneralLedger;

public sealed record TaxReportRateSummary(decimal CgstRate, decimal SgstRate, decimal TaxableAmount,
    decimal CgstAmount, decimal SgstAmount, decimal TotalTax);
public sealed record TaxReportSummary(DateOnly FromDate, DateOnly ToDate,
    IReadOnlyList<TaxReportRateSummary> Sales, IReadOnlyList<TaxReportRateSummary> Purchases,
    decimal SalesTaxable, decimal OutputTax, decimal PurchaseTaxable, decimal InputTaxCredit,
    decimal OutputTaxLedger, decimal InputTaxCreditLedger, decimal OutputTaxDifference,
    decimal InputTaxCreditDifference, decimal NetTaxPayable);
public sealed record GetTaxReportQuery(DateOnly? FromDate, DateOnly? ToDate) : IRequest<TaxReportSummary>;

/// <summary>GST transaction summary reconciled to the output GST and input credit control accounts.</summary>
public sealed class GetTaxReportHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<GetTaxReportQuery, TaxReportSummary>
{
    public async Task<TaxReportSummary> Handle(GetTaxReportQuery request, CancellationToken ct)
    {
        var to = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var fromDate = request.FromDate ?? new DateOnly(to.Year, 1, 1);
        if (fromDate > to) throw new ConflictException("From date must be on or before to date.");

        var result = await db.SalesProducts
         .Where(s => !s.IsDeleted)
         .Where(s => s.SaleDate >= fromDate && s.SaleDate <= to)
         .GroupBy(s => new { s.CgstRate, s.SgstRate })
         .Select(g => new
         {
             CgstRate = g.Key.CgstRate,
             SgstRate = g.Key.SgstRate,
             TaxableAmount = g.Sum(e => e.TaxableAmount),
             CgstAmount = g.Sum(e => e.CgstAmount),
             SgstAmount = g.Sum(e => e.SgstAmount)
         })
         .OrderBy(e0 => e0.CgstRate + e0.SgstRate)
         .ToListAsync();

            // Map to your custom domain object / DTO in memory
            var sales = result.Select(x => new TaxReportRateSummary(
                x.CgstRate,
                x.SgstRate,
                x.TaxableAmount,
                x.CgstAmount,
                x.SgstAmount,
                x.CgstAmount + x.SgstAmount // Total Tax
            )).ToList();
        //var sales = await db.SalesProducts.AsNoTracking()
        //    .Where(item => item.SaleDate >= fromDate && item.SaleDate <= to)
        //    .GroupBy(item => new { item.CgstRate, item.SgstRate })
        //    .Select(group => new TaxReportRateSummary(group.Key.CgstRate, group.Key.SgstRate,
        //        group.Sum(item => item.TaxableAmount), group.Sum(item => item.CgstAmount),
        //        group.Sum(item => item.SgstAmount), group.Sum(item => item.CgstAmount + item.SgstAmount)))
        //    .OrderBy(row => row.CgstRate + row.SgstRate).ToListAsync(ct);
        var purchaseRows = await db.Products.AsNoTracking()
            .Where(item => item.PurchaseDate >= fromDate && item.PurchaseDate <= to)
            .Select(item => new { item.Cgst, item.Sgst, item.PurchasePrice, item.Discount, item.TotalAmount })
            .ToListAsync(ct);
        var purchases = purchaseRows.GroupBy(item => new { item.Cgst, item.Sgst }).Select(group =>
        {
            var taxable = group.Sum(item => item.PurchasePrice - item.Discount);
            var cgst = group.Sum(item => decimal.Round((item.PurchasePrice - item.Discount) * item.Cgst / 100m, 2, MidpointRounding.AwayFromZero));
            var sgst = group.Sum(item => decimal.Round((item.PurchasePrice - item.Discount) * item.Sgst / 100m, 2, MidpointRounding.AwayFromZero));
            return new TaxReportRateSummary(group.Key.Cgst, group.Key.Sgst, taxable, cgst, sgst, group.Sum(item => item.TotalAmount - (item.PurchasePrice - item.Discount)));
        }).OrderBy(row => row.CgstRate + row.SgstRate).ToArray();

        async Task<(decimal Debit, decimal Credit)> LedgerMovement(string code)
        {
            var row = await (from line in db.JournalLines.AsNoTracking()
                             join entry in db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.Id
                             join account in db.ChartAccounts.AsNoTracking() on line.AccountId equals account.Id
                             where account.Code == code && entry.JournalDate >= fromDate && entry.JournalDate <= to
                             group line by account.Code into rows
                             select new { Debit = rows.Sum(line => line.Debit), Credit = rows.Sum(line => line.Credit) })
                .FirstOrDefaultAsync(ct);
            return (row?.Debit ?? 0m, row?.Credit ?? 0m);
        }

        var outputLedger = await LedgerMovement("2100");
        var inputLedger = await LedgerMovement("2200");
        var outputTax = sales.Sum(row => row.TotalTax);
        var inputTax = purchases.Sum(row => row.TotalTax);
        var outputLedgerNet = outputLedger.Credit - outputLedger.Debit;
        var inputLedgerNet = inputLedger.Debit - inputLedger.Credit;
        return new TaxReportSummary(fromDate, to, sales, purchases,
            sales.Sum(row => row.TaxableAmount), outputTax, purchases.Sum(row => row.TaxableAmount), inputTax,
            outputLedgerNet, inputLedgerNet, outputTax - outputLedgerNet, inputTax - inputLedgerNet,
            outputTax - inputTax);
    }
}
