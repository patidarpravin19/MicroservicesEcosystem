using MediatR;

namespace InventoryService.Application.StockItems.Commands.AddStock;

public sealed record AddStockCommand(string Sku, int Quantity) : IRequest<AddStockResult>;

public sealed record AddStockResult(Guid StockItemId, string Sku, int NewQuantityOnHand);
