using FluentValidation;

namespace AccountingInventory.Application.ProductTypes.Commands.UpdateProductTypeCommand;

public sealed class UpdateProductTypeCommandValidator : AbstractValidator<UpdateProductTypeCommand>
{
    public UpdateProductTypeCommandValidator()
    {
        RuleFor(x => x.VendorId).NotEqual(Guid.Empty);
        RuleFor(x => x.BrandId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

