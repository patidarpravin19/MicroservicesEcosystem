using FluentValidation;

namespace AccountingInventory.Application.Users.Commands.Refresh;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.ExpiredAccessToken).NotEmpty();
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
