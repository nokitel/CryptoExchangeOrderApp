using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.Queries;
using Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace Tests.QueryTests
{
    public class GetOrderBooksHandlerTests
    {
        private readonly Mock<IOrderBookRepository> _repositoryMock;
        private readonly GetOrderBooksHandler _handler;

        public GetOrderBooksHandlerTests()
        {
            _repositoryMock = new Mock<IOrderBookRepository>();

            // Seed mock data
            SeedOrderBooks();

            _handler = new GetOrderBooksHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_ReturnsOrderBooks()
        {
            // Arrange
            var query = new GetOrderBooksQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCountGreaterThan(0);
        }

        private void SeedOrderBooks()
        {
            var orderBook = new OrderBook
            {
                ExchangeName = "Exchange1",
                EurBalance = 50000m,
                BtcBalance = 10m
            };

            _repositoryMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<OrderBook> { orderBook });
        }
    }
}