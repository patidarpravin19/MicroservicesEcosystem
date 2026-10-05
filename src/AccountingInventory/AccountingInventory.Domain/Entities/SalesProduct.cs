using BuildingBlocks.Domain;

namespace AccountingInventory.Domain.Entities;

/// <summary>A tenant scoped product sale and its customer details.</summary>
public sealed class SalesProduct : AggregateRoot
{
    public string ProductId { get; private set; } = null!;
    public string CustomerName { get; private set; } = null!;
    public string CustomerMobile { get; private set; } = null!;
    public string CustomerAddress { get; private set; } = null!;
    public DateOnly SaleDate { get; private set; }
    public decimal ProductPrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public decimal Discount { get; private set; }

    public static SalesProduct Create(string productId,
        string customerName, string customerMobile, string customerAddress, DateOnly saleDate,
        decimal productPrice, decimal sellingPrice, decimal discount)
        => new()
        {
            Id = Guid.NewGuid(),
            IsActive = true,
            ProductId = productId.Trim(),
            CustomerName = customerName.Trim(),
            CustomerMobile = customerMobile.Trim(),
            CustomerAddress = customerAddress.Trim(),
            SaleDate = saleDate,
            ProductPrice = productPrice,
            SellingPrice = sellingPrice,
            Discount = discount
        };

    public void Update(string productId,
        string customerName, string customerMobile, string customerAddress, DateOnly saleDate,
        decimal productPrice, decimal sellingPrice, decimal discount)
    {
        ProductId = productId.Trim();
        CustomerName = customerName.Trim();
        CustomerMobile = customerMobile.Trim();
        CustomerAddress = customerAddress.Trim();
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
