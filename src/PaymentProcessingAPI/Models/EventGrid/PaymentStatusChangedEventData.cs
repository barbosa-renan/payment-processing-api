namespace PaymentProcessingAPI.Models.EventGrid;

public class PaymentStatusChangedEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public string PreviousStatus { get; set; } = string.Empty;
    public string NewStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}