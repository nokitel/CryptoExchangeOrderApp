using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Sell;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.SellTests
{
    public class SellBtcHandlerTests
    {
        private readonly Mock<IOrderBookRepository> _repositoryMock;
        private readonly SellBtcHandler _handler;
        private readonly Mock<ILogger<SellBtcHandler>> _loggerMock;

        public SellBtcHandlerTests()
        {
            _repositoryMock = new Mock<IOrderBookRepository>();
            _loggerMock = new Mock<ILogger<SellBtcHandler>>();

            // Seed mock data
            SeedOrderBooks();

            _handler = new SellBtcHandler(_repositoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsExecutionResult()
        {
            // Arrange
            var command = new SellBtcCommand(1.0m);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Orders.Should().NotBeEmpty();
            result.TotalRevenue.Should().BeGreaterThan(0);
        }

        private void SeedOrderBooks()
        {
            var orderBook = new OrderBook
            {
                ExchangeName = "Exchange1",
                EurBalance = 50000m,
                BtcBalance = 10m,
                Asks = new List<Order>
                {
                    new Order
                    {
                        Price = 51000m,
                        Amount = 2m,
                        OrderType = OrderType.Buy
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