using Domain.Entities;

namespace Application.Common.Interfaces
{
    public interface IOrderBookRepository
    {
        Task<List<OrderBook>> GetAllAsync(CancellationToken cancellationToken);
        Task<OrderBook> GetByIdAsync(Guid id, CancellationToken cancellationToken);
        Task AddAsync(OrderBook orderBook, CancellationToken cancellationToken);
        Task UpdateRangeAsync(IEnumerable<OrderBook> orderBooks, CancellationToken cancellationToken);
    }
}