using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using Domain.Entities;
using System.Threading;
using System.Threading.Tasks;


namespace Infrastructure.Data
{
    public class ExchangeContext : DbContext, IDbContext
    {
        public ExchangeContext(DbContextOptions<ExchangeContext> options)
            : base(options)
        {
        }

        public DbSet<OrderBook> OrderBooks { get; set; }
        public DbSet<Order> Orders { get; set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Order -> OrderBook (Many-to-One)
            modelBuilder.Entity<Order>()
                .HasOne(o => o.OrderBook)
                .WithMany(ob => ob.Bids) // Adjust if both `Bids` and `Asks` are relevant
                .HasForeignKey(o => o.OrderBookId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        // Explicit implementation of IDbContext
        public DbSet<T> Set<T>() where T : class
        {
            return base.Set<T>();
        }
    }
}