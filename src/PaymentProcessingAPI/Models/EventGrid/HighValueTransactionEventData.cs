namespace PaymentProcessingAPI.Models.EventGrid;

public class HighValueTransactionEventData
{
    public string TransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public int RiskScore { get; set; }
    public bool RequiresApproval { get; set; }
    public string CustomerTier { get; set; } = string.Empty;
}