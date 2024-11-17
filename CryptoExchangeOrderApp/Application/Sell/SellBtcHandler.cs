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

namespace Application.Sell
{
    public class SellBtcHandler : IRequestHandler<SellBtcCommand, ExecutionResult>
    {
        private readonly IOrderBookRepository _repository;
        private readonly ILogger<SellBtcHandler> _logger;

        public SellBtcHandler(IOrderBookRepository repository, ILogger<SellBtcHandler> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ExecutionResult> Handle(SellBtcCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var orderBooks = await _repository.GetAllAsync(cancellationToken);

                if (!orderBooks.Any())
                    throw new InvalidOperationException("No order books found.");

                // Aggregate all asks (buy orders) from all exchanges
                var allAsks = orderBooks
                    .SelectMany(ob => ob.Asks.Select(a => new { Ask = a, Exchange = ob }))
                    .Where(x => x.Ask.OrderType == OrderType.Buy)
                    .OrderByDescending(x => x.Ask.Price) // Highest price first
                    .ToList();

                decimal totalAmountToSell = request.Amount;
                decimal totalRevenue = 0;
                var ordersToExecute = new List<OrderExecution>();

                foreach (var ask in allAsks)
                {
                    if (totalAmountToSell <= 0)
                        break;

                    // Check exchange BTC balance limit
                    decimal availableAmount = Math.Min(ask.Ask.Amount, ask.Exchange.BtcBalance);

                    decimal amountToTrade = Math.Min(availableAmount, totalAmountToSell);

                    if (amountToTrade <= 0)
                        continue;

                    // Update totals
                    totalAmountToSell -= amountToTrade;
                    totalRevenue += amountToTrade * ask.Ask.Price;

                    // Update exchange balances
                    ask.Exchange.EurBalance += amountToTrade * ask.Ask.Price;
                    ask.Exchange.BtcBalance -= amountToTrade;

                    // Update order amount
                    ask.Ask.Amount -= amountToTrade;

                    // Prepare execution result
                    ordersToExecute.Add(new OrderExecution
                    {
                        Id = ask.Ask.Id,
                        ExchangeName = ask.Exchange.ExchangeName,
                        Amount = amountToTrade,
                        Price = ask.Ask.Price
                    });

                    // Remove order if fully executed
                    if (ask.Ask.Amount <= 0)
                    {
                        ask.Exchange.Asks.Remove(ask.Ask);
                    }
                }

                if (totalAmountToSell > 0)
                    throw new InvalidOperationException("Not enough liquidity to fulfill the sell order.");

                // Save changes via repository
                await _repository.UpdateRangeAsync(orderBooks, cancellationToken);

                return new ExecutionResult
                {
                    Orders = ordersToExecute,
                    TotalRevenue = totalRevenue
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing the sell order.");
                throw;
            }
        }
    }
}