namespace Domain.Entities
{
    public class OrderRequest
    {
        public decimal Amount { get; set; }
        public string OrderType { get; set; } = string.Empty;
    }
}