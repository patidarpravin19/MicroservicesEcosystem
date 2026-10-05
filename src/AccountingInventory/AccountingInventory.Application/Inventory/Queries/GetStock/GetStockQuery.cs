using AccountingInventory.Application.Common.Models;
using MediatR;

namespace AccountingInventory.Application.Inventory.Queries.GetStock;

public sealed record GetStockQuery(int Page = 1, int PageSize = 20, string? Search = null)
    : IRequest<PagedResult<StockItemSummary>>;

public sealed record StockItemSummary(
    Guid Id,
    string ProductName,
    string SerialNumber,
    string? SerialNumber1,
    int QuantityOnHand,
    decimal TotalAmount,
    bool IsSold,
    bool IsActive,
    string StockStatus);
