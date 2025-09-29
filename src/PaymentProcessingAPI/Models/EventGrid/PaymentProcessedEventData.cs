namespace PaymentProcessingAPI.Models.EventGrid;

public class PaymentProcessedEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
}
