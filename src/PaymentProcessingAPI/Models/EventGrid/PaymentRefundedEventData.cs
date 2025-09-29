namespace PaymentProcessingAPI.Models.EventGrid;

public class PaymentRefundedEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public string OriginalTransactionId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string RefundReason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
}