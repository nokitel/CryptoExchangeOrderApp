using FluentValidation;

namespace Application.Sell
{
    public class SellBtcCommandValidator : AbstractValidator<SellBtcCommand>
    {
        public SellBtcCommandValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");
        }
    }
}