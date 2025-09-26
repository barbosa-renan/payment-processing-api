namespace PaymentProcessingAPI.Configuration
{
    public class ServiceBusConfiguration
    {
        public string PaymentProcessedQueue { get; set; } = "payment-processed";
        public string PaymentFailedQueue { get; set; } = "payment-failed";
        public string NotificationsQueue { get; set; } = "notifications";
        public string RefundRequestsQueue { get; set; } = "refund-requests";
        public string HighValueApprovalQueue { get; set; } = "high-value-approval";
        
        // Configurações de retry
        public int MaxRetryAttempts { get; set; } = 3;
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);
    }
}