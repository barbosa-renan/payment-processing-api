namespace PaymentProcessingAPI.Constants;

public static class EventTypes
{
    public const string PaymentProcessed = "PaymentProcessing.Payment.Processed";
    public const string PaymentFailed = "PaymentProcessing.Payment.Failed";
    public const string HighValueTransaction = "PaymentProcessing.Transaction.HighValue";
    public const string PaymentRefunded = "PaymentProcessing.Payment.Refunded";
    public const string PaymentStatusChanged = "PaymentProcessing.Payment.StatusChanged";
    public const string PaymentCancelled = "PaymentProcessing.Payment.Cancelled";
}