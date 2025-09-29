namespace PaymentProcessingAPI.Models.EventGrid;

public class PaymentFailedEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public bool Retryable { get; set; }
}