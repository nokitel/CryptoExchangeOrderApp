using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class OrderBookRepository : IOrderBookRepository
    {
        private readonly IDbContext _context;

        public OrderBookRepository(IDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderBook>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Set<OrderBook>()
                .Include(ob => ob.Bids)
                .Include(ob => ob.Asks)
                .ToListAsync(cancellationToken);
        }

        public async Task<OrderBook> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Set<OrderBook>()
                .Include(ob => ob.Bids)
                .Include(ob => ob.Asks)
                .FirstOrDefaultAsync(ob => ob.Id == id, cancellationToken)
                ?? throw new KeyNotFoundException($"OrderBook with Id {id} was not found.");
        }

        public async Task AddAsync(OrderBook orderBook, CancellationToken cancellationToken)
        {
            await _context.Set<OrderBook>().AddAsync(orderBook, cancellationToken);
        }

        public async Task UpdateRangeAsync(IEnumerable<OrderBook> orderBooks, CancellationToken cancellationToken)
        {
            _context.Set<OrderBook>().UpdateRange(orderBooks);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}