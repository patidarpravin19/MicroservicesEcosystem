using MediatR;

namespace InventoryService.Application.StockItems.Commands.CreateStockItem;

public sealed record CreateStockItemCommand(
    string Sku,
    string DisplayName,
    string WarehouseLocation) : IRequest<CreateStockItemResult>;

public sealed record CreateStockItemResult(Guid StockItemId, string Sku, string DisplayName, int QuantityOnHand);
