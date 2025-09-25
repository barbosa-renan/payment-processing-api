namespace PaymentProcessingAPI.Models.ServiceBus
{
    public class NotificationMessage
    {
        public string CustomerId { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string NotificationType { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty; // EMAIL, SMS, PUSH
        public Dictionary<string, object> Data { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }
}