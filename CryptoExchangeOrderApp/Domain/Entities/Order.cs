using Domain.Enums;

namespace Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public OrderType OrderType { get; set; }
        public Guid OrderBookId { get; set; }
        public OrderBook OrderBook { get; set; }
    }
}