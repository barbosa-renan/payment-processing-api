namespace PaymentProcessingAPI.Models.ServiceBus
{
    public class PaymentFailedMessage
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime FailedAt { get; set; }
        public bool IsRetryable { get; set; }
    }
}