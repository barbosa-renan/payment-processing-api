namespace PaymentProcessingAPI.Configurations;

public class AzureServiceBusOptions
{
    public const string ConfigSectionName = "AzureServiceBus";
    
    public string ConnectionString { get; set; } = string.Empty;
    public string PaymentQueue { get; set; } = "payment-queue";
    public string RefundQueue { get; set; } = "refund-queue";
    public string NotificationQueue { get; set; } = "notification-queue";
    public string PaymentProcessedQueue { get; set; } = "payment-processed-queue";
    public string PaymentFailedQueue { get; set; } = "payment-failed-queue";
    public string NotificationsQueue { get; set; } = "notifications-queue";
    public string RefundRequestsQueue { get; set; } = "refund-requests-queue";
    public string HighValueApprovalQueue { get; set; } = "high-value-approval-queue";
}

public class AzureEventGridOptions
{
    public const string ConfigSectionName = "AzureEventGrid";
    
    public string TopicEndpoint { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
}

public class PaymentGatewayOptions
{
    public const string ConfigSectionName = "PaymentGateway";
    
    public string ApiKey { get; set; } = string.Empty;
    public string Environment { get; set; } = "sandbox"; // sandbox/production
    public int TimeoutSeconds { get; set; } = 30;
    public string BaseUrl { get; set; } = string.Empty;
}

public class JwtOptions
{
    public const string ConfigSectionName = "Jwt";
    
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}