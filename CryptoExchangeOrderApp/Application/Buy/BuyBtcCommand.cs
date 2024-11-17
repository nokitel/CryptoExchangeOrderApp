using MediatR;
using Domain.Entities;

namespace Application.Buy
{
    public class BuyBtcCommand : IRequest<ExecutionResult>
    {
        public decimal Amount { get; }

        public BuyBtcCommand(decimal amount)
        {
            Amount = amount;
        }
    }
}