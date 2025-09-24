namespace PaymentProcessingAPI.Models;

public class PaymentEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime EventTime { get; set; } = DateTime.UtcNow;
    public object Data { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string DataVersion { get; set; } = "1.0";
}

public static class PaymentEventTypes
{
    public const string PaymentProcessed = "Payment.Processed";
    public const string PaymentFailed = "Payment.Failed";
    public const string PaymentRefunded = "Payment.Refunded";
    public const string PaymentCancelled = "Payment.Cancelled";
    public const string HighValueTransaction = "Payment.HighValue";
}