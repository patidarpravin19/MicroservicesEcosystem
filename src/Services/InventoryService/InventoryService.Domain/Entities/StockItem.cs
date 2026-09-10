using InventoryService.Domain.Common;
using InventoryService.Domain.Events;
using InventoryService.Domain.Exceptions;

namespace InventoryService.Domain.Entities;

/// <summary>
/// Aggregate root for the Inventory bounded context. All mutation of on-hand
/// quantity happens through explicit behavior methods so invariants (quantity can
/// never go negative, SKU is immutable) are enforced in exactly one place.
/// </summary>
public sealed class StockItem : AggregateRoot
{
    public required string Sku { get; init; }
    public required string DisplayName { get; init; }
    public int QuantityOnHand { get; private set; }
    public string WarehouseLocation { get; private set; } = "DEFAULT";

    public static StockItem Create(string sku, string displayName, string warehouseLocation)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new InventoryDomainException("SKU cannot be empty.");
        }

        return new StockItem
        {
            Id = Guid.NewGuid(),
            Sku = sku.ToUpperInvariant(),
            DisplayName = displayName,
            WarehouseLocation = string.IsNullOrWhiteSpace(warehouseLocation) ? "DEFAULT" : warehouseLocation,
        };
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException("Quantity to add must be a positive number.");
        }

        QuantityOnHand += quantity;

        RaiseDomainEvent(new StockAddedDomainEvent(
            Id, Sku, quantity, QuantityOnHand, DateTimeOffset.UtcNow));
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new InventoryDomainException("Quantity to remove must be a positive number.");
        }

        if (quantity > QuantityOnHand)
        {
            throw new InventoryDomainException(
                $"Cannot remove {quantity} units — only {QuantityOnHand} units of '{Sku}' are on hand.");
        }

        QuantityOnHand -= quantity;
    }

    private StockItem() { }
}
