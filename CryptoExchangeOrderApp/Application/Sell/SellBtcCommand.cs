using MediatR;
using Domain.Entities;

namespace Application.Sell
{
    public class SellBtcCommand : IRequest<ExecutionResult>
    {
        public decimal Amount { get; }

        public SellBtcCommand(decimal amount)
        {
            Amount = amount;
        }
    }
}