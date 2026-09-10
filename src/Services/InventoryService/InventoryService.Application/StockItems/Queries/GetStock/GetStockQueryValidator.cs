using FluentValidation;

namespace InventoryService.Application.StockItems.Queries.GetStock;

public sealed class GetStockQueryValidator : AbstractValidator<GetStockQuery>
{
    public GetStockQueryValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
    }
}
