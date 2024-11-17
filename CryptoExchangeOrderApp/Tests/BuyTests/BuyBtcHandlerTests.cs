using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Buy;
using Application.Common.Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.BuyTests
{
    public class BuyBtcHandlerTests
    {
        private readonly Mock<IOrderBookRepository> _repositoryMock;
        private readonly BuyBtcHandler _handler;
        private readonly Mock<ILogger<BuyBtcHandler>> _loggerMock;

        public BuyBtcHandlerTests()
        {
            _repositoryMock = new Mock<IOrderBookRepository>();
            _loggerMock = new Mock<ILogger<BuyBtcHandler>>();

            // Seed mock data
            SeedOrderBooks();

            _handler = new BuyBtcHandler(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsExecutionResult()
        {
            // Arrange
            var command = new BuyBtcCommand(1.0m);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Orders.Should().NotBeEmpty();
            result.TotalCost.Should().BeGreaterThan(0);
        }

        private void SeedOrderBooks()
        {
            var orderBook = new OrderBook
            {
                ExchangeName = "Exchange1",
                EurBalance = 100000m,
                BtcBalance = 50m,
                Bids = new List<Order>
                {
                    new Order
                    {
                        Price = 50000m,
                        Amount = 2m,
                        OrderType = OrderType.Sell
                    }
                }
            };

            _repositoryMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderBook> { orderBook });

            _repositoryMock.Setup(repo => repo.UpdateRangeAsync(It.IsAny<IEnumerable<OrderBook>>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }
    }
}