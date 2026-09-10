using FluentValidation;

namespace InventoryService.Application.StockItems.Commands.CreateStockItem;

public sealed class CreateStockItemCommandValidator : AbstractValidator<CreateStockItemCommand>
{
    public CreateStockItemCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Sku must contain only letters, digits, and hyphens.");

        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.WarehouseLocation).MaximumLength(64);
    }
}
