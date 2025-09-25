namespace PaymentProcessingAPI.Models.ServiceBus
{
    public class PaymentProcessedMessage
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string AuthorizationCode { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public List<OrderItem> Items { get; set; } = new();
    }
}