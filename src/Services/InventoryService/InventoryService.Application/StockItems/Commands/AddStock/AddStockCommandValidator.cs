using FluentValidation;

namespace InventoryService.Application.StockItems.Commands.AddStock;

public sealed class AddStockCommandValidator : AbstractValidator<AddStockCommand>
{
    public AddStockCommandValidator()
    {
        RuleFor(x => x.Sku)
            .NotEmpty()
            .MaximumLength(64)
            .Matches("^[A-Za-z0-9-]+$")
            .WithMessage("Sku must contain only letters, digits, and hyphens.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity to add must be greater than zero.");
    }
}
