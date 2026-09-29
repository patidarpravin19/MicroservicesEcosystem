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
    public int Quantity { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Cgst { get; private set; }
    public decimal Sgst { get; private set; }
    public decimal Tax { get; private set; }

    public static Product Create(Guid vendorId, Guid brandId, Guid productTypeId, Guid productModelId,
        Guid variantId, Guid colorId, string serialNumber, string? serialNumber1, int quantity,
        decimal purchasePrice, decimal discount, decimal cgst, decimal sgst, decimal tax)
    {
        Validate(vendorId, brandId, productTypeId, productModelId, variantId, colorId,
            serialNumber, quantity, purchasePrice, discount, cgst, sgst, tax);
        return new Product
        {
            Id = Guid.NewGuid(), IsActive = true, VendorId = vendorId, BrandId = brandId,
            ProductTypeId = productTypeId, ProductModelId = productModelId, VariantId = variantId,
            ColorId = colorId, SerialNumber = serialNumber.Trim(),
            SerialNumber1 = string.IsNullOrWhiteSpace(serialNumber1) ? null : serialNumber1.Trim(),
            Quantity = quantity, PurchasePrice = purchasePrice, Discount = discount,
            Cgst = cgst, Sgst = sgst, Tax = tax
        };
    }

    public void Update(Guid vendorId, Guid brandId, Guid productTypeId, Guid productModelId,
        Guid variantId, Guid colorId, string serialNumber, string? serialNumber1, int quantity,
        decimal purchasePrice, decimal discount, decimal cgst, decimal sgst, decimal tax)
    {
        Validate(vendorId, brandId, productTypeId, productModelId, variantId, colorId,
            serialNumber, quantity, purchasePrice, discount, cgst, sgst, tax);
        VendorId = vendorId; BrandId = brandId; ProductTypeId = productTypeId;
        ProductModelId = productModelId; VariantId = variantId; ColorId = colorId;
        SerialNumber = serialNumber.Trim();
        SerialNumber1 = string.IsNullOrWhiteSpace(serialNumber1) ? null : serialNumber1.Trim();
        Quantity = quantity; PurchasePrice = purchasePrice; Discount = discount;
        Cgst = cgst; Sgst = sgst; Tax = tax;
    }

    public void Delete() { IsDeleted = true; IsActive = false; }

    private static void Validate(Guid vendorId, Guid brandId, Guid productTypeId, Guid productModelId,
        Guid variantId, Guid colorId, string serialNumber, int quantity, decimal purchasePrice,
        decimal discount, decimal cgst, decimal sgst, decimal tax)
    {
        if (vendorId == Guid.Empty || brandId == Guid.Empty || productTypeId == Guid.Empty ||
            productModelId == Guid.Empty || variantId == Guid.Empty || colorId == Guid.Empty)
            throw new ArgumentException("All product reference IDs are required.");
        if (string.IsNullOrWhiteSpace(serialNumber)) throw new ArgumentException("SerialNumber is required.");
        if (quantity <= 0) throw new ArgumentException("Quantity must be greater than zero.");
        if (purchasePrice < 0 || discount < 0 || cgst < 0 || sgst < 0 || tax < 0)
            throw new ArgumentException("Price, discount, and tax values cannot be negative.");
    }

    private Product() { }
}
