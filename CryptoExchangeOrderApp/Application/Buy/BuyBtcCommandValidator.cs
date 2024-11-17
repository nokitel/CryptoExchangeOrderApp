using FluentValidation;

namespace Application.Buy
{
    public class BuyBtcCommandValidator : AbstractValidator<BuyBtcCommand>
    {
        public BuyBtcCommandValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0)
                .WithMessage("Amount must be greater than zero.");
        }
    }
}