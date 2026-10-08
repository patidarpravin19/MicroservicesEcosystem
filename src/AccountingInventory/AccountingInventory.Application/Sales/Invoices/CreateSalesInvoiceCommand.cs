using AccountingInventory.Application.Abstractions;
using AccountingInventory.Application.Common.Models;
using AccountingInventory.Application.GeneralLedger;
using AccountingInventory.Application.Sales.Products;
using AccountingInventory.Domain.Entities;
using BuildingBlocks.Application.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AccountingInventory.Application.Sales.Invoices;

public sealed record CreateSalesInvoiceCommand(
    string CustomerName,
    string CustomerMobile,
    string CustomerAddress,
    string? CustomerEmail,
    DateOnly InvoiceDate,
    int PaymentTermsDays,
    string? Notes,
    IReadOnlyList<CreateSalesInvoiceLineCommandDto> Lines,
    CreateSalesInvoiceInitialPaymentDto? InitialPayment = null,
    string? CustomerGstin = null,
    string? PlaceOfSupplyStateCode = null,
    string? PlaceOfSupplyStateName = null,
    GstSupplyType? SupplyType = null) : IRequest<SalesInvoiceDetailsDto>;

public sealed class CreateSalesInvoiceCommandHandler(IAccountingInventoryDbContext db)
    : IRequestHandler<CreateSalesInvoiceCommand, SalesInvoiceDetailsDto>
{
    public async Task<SalesInvoiceDetailsDto> Handle(CreateSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request.Lines == null || request.Lines.Count == 0)
            throw new ArgumentException("At least one invoice line item is required.");

        var seller = await db.CustomerBillSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

        // Resolve Place of Supply and customer GST details
        var customerGstin = !string.IsNullOrWhiteSpace(request.CustomerGstin) ? request.CustomerGstin.Trim().ToUpperInvariant() : null;
        var posCode = GstStates.ExtractStateCode(request.PlaceOfSupplyStateCode)
            ?? GstStates.ExtractStateCode(customerGstin)
            ?? GstStates.ExtractStateCode(request.CustomerAddress);

        var posName = !string.IsNullOrWhiteSpace(request.PlaceOfSupplyStateName)
            ? request.PlaceOfSupplyStateName.Trim()
            : GstStates.GetStateName(posCode);

        // Determine Intra-State (CGST+SGST) vs Inter-State (IGST)
        var supplierState = seller?.StateCode ?? GstStates.ExtractStateCode(seller?.TaxRegistrationNumber) ?? GstStates.ExtractStateCode(seller?.CompanyAddress);
        var effectiveSupplyType = request.SupplyType ?? GstStates.DetermineSupplyType(supplierState, posCode);

        var customer = await CustomerResolver.GetOrCreateAsync(
            db,
            request.CustomerName,
            request.CustomerMobile,
            request.CustomerAddress,
            request.CustomerEmail,
            cancellationToken,
            posCode,
            posName,
            customerGstin);

        // Process serialized products in the lines
        var serializedProductIds = request.Lines
            .Where(l => l.ProductId.HasValue && l.ProductId.Value != Guid.Empty)
            .Select(l => l.ProductId!.Value)
            .Distinct()
            .ToList();

        var productsToRelieve = new List<Product>();

        if (serializedProductIds.Count > 0)
        {
            var foundProducts = await db.Products
                .Where(p => serializedProductIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            foreach (var prodId in serializedProductIds)
            {
                var prod = foundProducts.FirstOrDefault(p => p.Id == prodId)
                    ?? throw new NotFoundException($"Product with ID '{prodId}' was not found.");

                if (!prod.IsActive || prod.IsSold)
                    throw new ConflictException($"Product '{prod.SerialNumber}' has already been sold or is inactive.");

                if (request.InvoiceDate < prod.PurchaseDate)
                    throw new ConflictException($"Sale date {request.InvoiceDate} cannot precede purchase date {prod.PurchaseDate} for product '{prod.SerialNumber}'.");

                prod.MarkSold();
                productsToRelieve.Add(prod);
            }
        }

        var billNumber = await db.GenerateSalesBillNumberAsync(request.InvoiceDate.Year, cancellationToken);

        var drafts = request.Lines.Select(l =>
        {
            decimal cgstRate = 0m;
            decimal sgstRate = 0m;
            decimal igstRate = 0m;

            if (effectiveSupplyType == GstSupplyType.InterState)
            {
                // Inter-state supply -> entire tax applies as IGST
                igstRate = l.IgstRate > 0 ? l.IgstRate : (l.CgstRate + l.SgstRate);
                cgstRate = 0m;
                sgstRate = 0m;
            }
            else
            {
                // Intra-state supply -> split tax equally into CGST + SGST
                if (l.CgstRate == 0 && l.SgstRate == 0 && l.IgstRate > 0)
                {
                    cgstRate = decimal.Round(l.IgstRate / 2m, 2, MidpointRounding.AwayFromZero);
                    sgstRate = decimal.Round(l.IgstRate / 2m, 2, MidpointRounding.AwayFromZero);
                    igstRate = 0m;
                }
                else
                {
                    cgstRate = l.CgstRate;
                    sgstRate = l.SgstRate;
                    igstRate = 0m;
                }
            }

            return new SalesInvoiceLineDraft(
                l.ItemType,
                l.ProductId,
                l.ItemDescription,
                l.SerialNumber,
                l.SerialNumber1,
                l.Quantity,
                l.UnitPrice,
                l.Discount,
                l.TaxId,
                cgstRate,
                sgstRate,
                igstRate);
        }).ToList();

        var invoice = SalesInvoice.Create(
            billNumber,
            customer.Id,
            request.InvoiceDate,
            request.PaymentTermsDays,
            request.Notes,
            drafts,
            effectiveSupplyType,
            posCode,
            posName,
            customerGstin);

        db.SalesInvoices.Add(invoice);

        // General Ledger posting for the multi-line invoice
        var ledgerAccounts = await LedgerPosting.EnsureSystemAccountsAsync(db, cancellationToken);
        var saleJournal = LedgerPosting.ForSalesInvoice(invoice, productsToRelieve, ledgerAccounts);
        if (saleJournal is not null)
        {
            LedgerPosting.Add(db, saleJournal);
        }

        await LedgerPosting.EnsurePeriodOpenAsync(db, invoice.InvoiceDate, cancellationToken);

        SalesReceipt? receipt = null;
        if (request.InitialPayment != null && request.InitialPayment.Amount > 0)
        {
            invoice.RecordPayment(request.InitialPayment.Amount);
            receipt = SalesReceipt.Create(
                invoice.Id,
                request.InitialPayment.Amount,
                request.InitialPayment.PaymentMode,
                request.InitialPayment.PaymentDate,
                request.InitialPayment.ReferenceNumber,
                request.InitialPayment.Note);

            db.SalesReceipts.Add(receipt);
            LedgerPosting.Add(db, LedgerPosting.ForSalesReceipt(receipt, ledgerAccounts));
            await LedgerPosting.EnsurePeriodOpenAsync(db, receipt.PaymentDate, cancellationToken);
        }

        // Capture immutable invoice snapshot for printing and history
        var itemsSummary = string.Join("; ", invoice.Lines.Select(l => $"{l.ItemDescription} (x{l.Quantity})"));
        var serialsSummary = string.Join(", ", invoice.Lines.Where(l => !string.IsNullOrWhiteSpace(l.SerialNumber)).Select(l => l.SerialNumber));

        db.InvoiceSnapshots.Add(InvoiceSnapshot.Capture(
            "Sale",
            invoice.Id,
            customer.Name,
            customer.Mobile,
            customer.Address,
            customer.Email,
            itemsSummary,
            serialsSummary,
            JsonSerializer.Serialize(new
            {
                BillNumber = invoice.BillNumber,
                SaleDate = invoice.InvoiceDate,
                DueDate = invoice.DueDate,
                SupplyType = invoice.SupplyType,
                PlaceOfSupplyStateCode = invoice.PlaceOfSupplyStateCode,
                PlaceOfSupplyStateName = invoice.PlaceOfSupplyStateName,
                CustomerGstin = invoice.CustomerGstin,
                SubTotal = invoice.SubTotal,
                Discount = invoice.Discount,
                TaxableAmount = invoice.TaxableAmount,
                CgstAmount = invoice.CgstAmount,
                SgstAmount = invoice.SgstAmount,
                IgstAmount = invoice.IgstAmount,
                TotalAmount = invoice.TotalAmount,
                Seller = seller,
                Lines = invoice.Lines.Select(l => new
                {
                    l.LineNumber,
                    l.ItemDescription,
                    l.SerialNumber,
                    l.SerialNumber1,
                    l.Quantity,
                    l.UnitPrice,
                    l.Discount,
                    l.CgstRate,
                    l.SgstRate,
                    l.IgstRate,
                    l.TaxableAmount,
                    l.CgstAmount,
                    l.SgstAmount,
                    l.IgstAmount,
                    l.TotalAmount
                })
            }, new JsonSerializerOptions(JsonSerializerDefaults.Web))));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Stock changed while the sale was being saved. Refresh and try again.");
        }

        var summary = new SalesInvoiceSummaryDto(
            invoice.Id,
            invoice.BillNumber,
            customer.Id,
            customer.Name,
            customer.Mobile,
            customer.Address,
            customer.Email,
            invoice.InvoiceDate,
            invoice.PaymentTermsDays,
            invoice.DueDate,
            invoice.Lines.Count,
            invoice.SubTotal,
            invoice.Discount,
            invoice.TaxableAmount,
            invoice.CgstAmount,
            invoice.SgstAmount,
            invoice.IgstAmount,
            invoice.TotalAmount,
            invoice.AmountPaid,
            invoice.Balance,
            invoice.PaymentStatus,
            invoice.Notes,
            invoice.SupplyType,
            invoice.PlaceOfSupplyStateCode,
            invoice.PlaceOfSupplyStateName,
            invoice.CustomerGstin);

        var lineDtos = invoice.Lines.Select(l => new SalesInvoiceLineDto(
            l.Id,
            l.LineNumber,
            l.ItemType,
            l.ProductId,
            l.ItemDescription,
            l.SerialNumber,
            l.SerialNumber1,
            l.Quantity,
            l.UnitPrice,
            l.Discount,
            l.TaxId,
            l.CgstRate,
            l.SgstRate,
            l.IgstRate,
            l.TaxableAmount,
            l.CgstAmount,
            l.SgstAmount,
            l.IgstAmount,
            l.TotalAmount)).ToList();

        var paymentDtos = receipt is not null
            ? new List<SalesInvoiceReceiptDto>
            {
                new(receipt.Id, receipt.Amount, receipt.PaymentMode, receipt.PaymentDate, receipt.ReferenceNumber, receipt.Note)
            }
            : new List<SalesInvoiceReceiptDto>();

        return new SalesInvoiceDetailsDto(summary, lineDtos, paymentDtos);
    }
}

