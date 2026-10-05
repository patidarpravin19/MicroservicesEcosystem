using FluentValidation;

namespace AccountingInventory.Application.Sales.Products.Commands.UpdateSalesProduct;

public sealed class UpdateSalesProductCommandValidator : AbstractValidator<UpdateSalesProductCommand>
{
    public UpdateSalesProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ProductId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerMobile).NotEmpty().MaximumLength(20);
        RuleFor(x => x.CustomerAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.ProductPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SellingPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
    }
}
