using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Buy
{
    public class BuyBtcHandler : IRequestHandler<BuyBtcCommand, ExecutionResult>
    {
        private readonly IOrderBookRepository _orderBookRepository;
        private readonly ILogger<BuyBtcHandler> _logger;

        public BuyBtcHandler(IOrderBookRepository orderBookRepository, ILogger<BuyBtcHandler> logger)
        {
            _orderBookRepository = orderBookRepository;
            _logger = logger;
        }

        public async Task<ExecutionResult> Handle(BuyBtcCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Fetch all order books
                var orderBooks = await _orderBookRepository.GetAllAsync(cancellationToken);

                if (!orderBooks.Any())
                    throw new InvalidOperationException("No order books found.");

                // Aggregate all bids (sell orders) from all exchanges
                var allBids = orderBooks
                    .SelectMany(ob => ob.Bids.Select(b => new { Bid = b, Exchange = ob }))
                    .Where(x => x.Bid.OrderType == OrderType.Sell)
                    .OrderBy(x => x.Bid.Price) // Lowest price first
                    .ToList();

                decimal totalAmountNeeded = request.Amount;
                decimal totalCost = 0;
                var ordersToExecute = new List<OrderExecution>();

                foreach (var bid in allBids)
                {
                    if (totalAmountNeeded <= 0)
                        break;

                    // Check exchange EUR balance limit
                    decimal maxAffordableAmount = bid.Exchange.EurBalance / bid.Bid.Price;
                    decimal availableAmount = Math.Min(bid.Bid.Amount, maxAffordableAmount);

                    decimal amountToTrade = Math.Min(availableAmount, totalAmountNeeded);

                    if (amountToTrade <= 0)
                        continue;

                    // Update totals
                    totalAmountNeeded -= amountToTrade;
                    totalCost += amountToTrade * bid.Bid.Price;

                    // Update exchange balances
                    bid.Exchange.EurBalance -= amountToTrade * bid.Bid.Price;
                    bid.Exchange.BtcBalance += amountToTrade;

                    // Update order amount
                    bid.Bid.Amount -= amountToTrade;

                    // Prepare execution result
                    ordersToExecute.Add(new OrderExecution
                    {
                        Id = bid.Bid.Id,
                        ExchangeName = bid.Exchange.ExchangeName,
                        Amount = amountToTrade,
                        Price = bid.Bid.Price
                    });

                    // Remove order if fully executed
                    if (bid.Bid.Amount <= 0)
                    {
                        bid.Exchange.Bids.Remove(bid.Bid);
                    }
                }

                if (totalAmountNeeded > 0)
                    throw new InvalidOperationException("Not enough liquidity to fulfill the buy order.");

                // Save changes via repository
                await _orderBookRepository.UpdateRangeAsync(orderBooks, cancellationToken);

                return new ExecutionResult
                {
                    Orders = ordersToExecute,
                    TotalCost = totalCost
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operation was canceled.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while handling BuyBtcCommand.");
                throw;
            }
        }
    }
}