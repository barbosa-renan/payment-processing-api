namespace PaymentProcessingAPI.Models.ServiceBus
{
    public class HighValueApprovalMessage
    {
        public string TransactionId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public int RiskScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}