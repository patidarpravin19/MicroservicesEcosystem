using BuildingBlocks.Domain;

namespace AccountingInventory.Domain.Entities;

/// <summary>A product purchased from a vendor, including its identifying serials and cost.</summary>
public sealed class Product : AggregateRoot
{
    public Guid VendorId { get; private set; }
    public Guid BrandId { get; private set; }
    public Guid ProductTypeId { get; private set; }
    public Guid ProductModelId { get; private set; }
    public Guid VariantId { get; private set; }
    public Guid ColorId { get; private set; }
    public string SerialNumber { get; private set; } = null!;
    public string? SerialNumber1 { get; private set; }
    public string? BillNumber { get; private set; }
    public bool IsSold { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Cgst { get; private set; }
    public decimal Sgst { get; private set; }
    public decimal Tax { get; private set; }

    public static Product Create(Guid vendorId, Guid brandId, Guid productTypeId, Guid productModelId,
        Guid variantId, Guid colorId, string serialNumber, string? serialNumber1, string? billNumber,
        decimal purchasePrice, decimal discount, decimal cgst, decimal sgst, decimal tax)
    {
        Validate(vendorId, brandId, productTypeId, productModelId, variantId, colorId,
            serialNumber, purchasePrice, discount, cgst, sgst, tax);
        return new Product
        {
            Id = Guid.NewGuid(), IsActive = true, VendorId = vendorId, BrandId = brandId,
            ProductTypeId = productTypeId, ProductModelId = productModelId, VariantId = variantId,
            ColorId = colorId, SerialNumber = serialNumber.Trim(),
            SerialNumber1 = string.IsNullOrWhiteSpace(serialNumber1) ? null : serialNumber1.Trim(),
            BillNumber = NormalizeBillNumber(billNumber), IsSold = false,
            PurchasePrice = purchasePrice, Discount = discount,
            Cgst = cgst, Sgst = sgst, Tax = tax,
            TotalAmount = CalculateTotalAmount(purchasePrice, cgst, sgst)
        };
    }

    public void Update(Guid vendorId, Guid brandId, Guid productTypeId, Guid productModelId,
        Guid variantId, Guid colorId, string serialNumber, string? serialNumber1, string? billNumber,
        decimal purchasePrice, decimal discount, decimal cgst, decimal sgst, decimal tax)
    {
        Validate(vendorId, brandId, productTypeId, productModelId, variantId, colorId,
            serialNumber, purchasePrice, discount, cgst, sgst, tax);
        VendorId = vendorId; BrandId = brandId; ProductTypeId = productTypeId;
        ProductModelId = productModelId; VariantId = variantId; ColorId = colorId;
        SerialNumber = serialNumber.Trim();
        SerialNumber1 = string.IsNullOrWhiteSpace(serialNumber1) ? null : serialNumber1.Trim();
        BillNumber = NormalizeBillNumber(billNumber);
        PurchasePrice = purchasePrice; Discount = discount;
        Cgst = cgst; Sgst = sgst; Tax = tax;
        TotalAmount = CalculateTotalAmount(purchasePrice, cgst, sgst);
    }

    public void Delete() { IsDeleted = true; IsActive = false; }

    public void MarkSold()
    {
        if (IsSold) throw new InvalidOperationException("This product has already been sold.");
        IsSold = true;
    }

    public void MarkAvailable() => IsSold = false;

    private static string? NormalizeBillNumber(string? billNumber)
        => string.IsNullOrWhiteSpace(billNumber) ? null : billNumber.Trim();

    private static void Validate(Guid vendorId, Guid brandId, Guid productTypeId, Guid productModelId,
        Guid variantId, Guid colorId, string serialNumber, decimal purchasePrice,
        decimal discount, decimal cgst, decimal sgst, decimal tax)
    {
        if (vendorId == Guid.Empty || brandId == Guid.Empty || productTypeId == Guid.Empty ||
            productModelId == Guid.Empty || variantId == Guid.Empty || colorId == Guid.Empty)
            throw new ArgumentException("All product reference IDs are required.");
        if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("SerialNumber is required.");
        if (purchasePrice < 0 || discount < 0 || cgst < 0 || sgst < 0 || tax < 0)
            throw new ArgumentException("Price, discount, and tax values cannot be negative.");
    }

    private static decimal CalculateTotalAmount(decimal purchasePrice, decimal cgst, decimal sgst)
        => decimal.Round(purchasePrice * (1 + (cgst + sgst) / 100), 2, MidpointRounding.AwayFromZero);

    private Product() { }
}
