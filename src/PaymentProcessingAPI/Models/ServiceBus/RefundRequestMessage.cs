namespace PaymentProcessingAPI.Models.ServiceBus
{
    public class RefundRequestMessage
    {
        public string RefundId { get; set; } = string.Empty;
        public string OriginalTransactionId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal RefundAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
    }
}