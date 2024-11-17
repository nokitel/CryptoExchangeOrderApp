using System.Collections.Generic;

namespace Domain.Entities
{
    public class OrderBook
    {
        public Guid Id { get; set; }
        public string ExchangeName { get; set; } = string.Empty;        
        public decimal EurBalance { get; set; }
        public decimal BtcBalance { get; set; }
        public ICollection<Order> Bids { get; set; } = new List<Order>();
        public ICollection<Order> Asks { get; set; } = new List<Order>();
    }
}