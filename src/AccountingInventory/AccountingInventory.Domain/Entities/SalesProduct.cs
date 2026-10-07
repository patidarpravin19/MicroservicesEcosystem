using BuildingBlocks.Domain;

namespace AccountingInventory.Domain.Entities;

/// <summary>A tenant scoped product sale linked to a customer.</summary>
public sealed class SalesProduct : AggregateRoot
{
    public string BillNumber { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public DateOnly SaleDate { get; private set; }
    public decimal ProductPrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public decimal Discount { get; private set; }

    public static SalesProduct Create(string billNumber, string productId,
        Guid customerId, DateOnly saleDate,
        decimal productPrice, decimal sellingPrice, decimal discount)
        => new()
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            BillNumber = billNumber.Trim(),
            ProductId = productId.Trim(),
            CustomerId = customerId,
            SaleDate = saleDate,
            ProductPrice = productPrice,
            SellingPrice = sellingPrice,
            Discount = discount
        };

    public void Update(string productId,
        Guid customerId, DateOnly saleDate,
        decimal productPrice, decimal sellingPrice, decimal discount)
    {
        ProductId = productId.Trim();
        CustomerId = customerId;
        SaleDate = saleDate;
        ProductPrice = productPrice;
        SellingPrice = sellingPrice;
        Discount = discount;
    }

    public void Delete()
    {
        if (IsDeleted) return;
        IsDeleted = true;
        IsActive = false;
    }

    private SalesProduct() { }
}
