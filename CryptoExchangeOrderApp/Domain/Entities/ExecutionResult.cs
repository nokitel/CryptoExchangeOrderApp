using System.Collections.Generic;

namespace Domain.Entities
{
    public class ExecutionResult
    {
        public List<OrderExecution> Orders { get; set; } = new List<OrderExecution>();
        public decimal TotalCost { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class OrderExecution
    {
        public int Id { get; set; }
        public string ExchangeName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Price { get; set; }
    }
}