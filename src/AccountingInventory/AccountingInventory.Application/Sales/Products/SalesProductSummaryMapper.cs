using AccountingInventory.Application.Abstractions;
using AccountingInventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountingInventory.Application.Sales.Products;

internal static class SalesProductSummaryMapper
{
    public static async Task<IReadOnlyList<SalesProductSummary>> MapAsync(
        IAccountingInventoryDbContext db,
        IReadOnlyCollection<SalesProduct> sales,
        CancellationToken cancellationToken)
    {
        var productIds = sales
            .Select(sale => Guid.TryParse(sale.ProductId, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var products = await db.Products.AsNoTracking()
            .Where(product => productIds.Contains(product.Id))
            .Select(product => new
            {
                product.Id,
                Brand = db.Brands.Where(item => item.Id == product.BrandId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty,
                Model = db.ProductModels.Where(item => item.Id == product.ProductModelId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty,
                Variant = db.Variants.Where(item => item.Id == product.VariantId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty,
                Color = db.Colors.Where(item => item.Id == product.ColorId)
                    .Select(item => item.Name).FirstOrDefault() ?? string.Empty,
                product.SerialNumber,
                product.SerialNumber1
            })
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        var saleIds = sales.Select(sale => sale.Id).ToArray();
        var customerIds = sales.Select(sale => sale.CustomerId).Distinct().ToArray();
        var customers = await db.Customers.AsNoTracking()
            .Where(customer => customerIds.Contains(customer.Id))
            .ToDictionaryAsync(customer => customer.Id, cancellationToken);
        var paymentModes = await db.SalesPayments.AsNoTracking()
            .Where(payment => saleIds.Contains(payment.SalesProductId))
            .ToDictionaryAsync(payment => payment.SalesProductId, payment => payment.PaymentMode, cancellationToken);

        return sales.Select(sale =>
        {
            var product = Guid.TryParse(sale.ProductId, out var id) && products.TryGetValue(id, out var match)
                ? match
                : null;
            var serialNumber = product?.SerialNumber ?? product?.SerialNumber1 ?? string.Empty;
            customers.TryGetValue(sale.CustomerId, out var customer);
            var productName = product is null
                ? string.Empty
                : string.Join(" - ", new[] { product.Brand, product.Model, product.Variant, product.Color }
                    .Where(part => !string.IsNullOrWhiteSpace(part)));
            if (!string.IsNullOrWhiteSpace(serialNumber))
                productName = $"{productName} — {serialNumber}";

            return new SalesProductSummary(
                sale.Id,
                sale.ProductId,
                productName,
                serialNumber,
                sale.CustomerId,
                customer?.Name ?? string.Empty,
                customer?.Mobile ?? string.Empty,
                customer?.Address ?? string.Empty,
                customer?.Email,
                sale.SaleDate,
                sale.PaymentTermsDays,
                sale.DueDate,
                sale.ProductPrice,
                sale.SellingPrice,
                sale.Discount,
                sale.TaxId,
                sale.CgstRate,
                sale.SgstRate,
                sale.TaxableAmount,
                sale.CgstAmount,
                sale.SgstAmount,
                sale.TotalAmount,
                sale.IsActive,
                paymentModes.GetValueOrDefault(sale.Id),
                sale.BillNumber);
        }).ToArray();
    }
}
