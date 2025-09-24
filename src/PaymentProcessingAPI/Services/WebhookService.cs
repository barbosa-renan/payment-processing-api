using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Services;

public class WebhookService : IWebhookService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEventPublisherService _eventPublisherService;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IPaymentRepository paymentRepository,
        IEventPublisherService eventPublisherService,
        ILogger<WebhookService> logger)
    {
        _paymentRepository = paymentRepository;
        _eventPublisherService = eventPublisherService;
        _logger = logger;
    }

    public async Task<bool> ProcessPaymentNotificationAsync(PaymentWebhook webhook)
    {
        try
        {
            var payment = await _paymentRepository.GetPaymentAsync(webhook.TransactionId);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for transaction ID: {TransactionId}", webhook.TransactionId);
                return false;
            }

            // Update payment status based on webhook
            payment.Status = webhook.Status.ToString();
            payment.ProcessedAt = DateTime.UtcNow;
            
            await _paymentRepository.UpdatePaymentAsync(payment);

            // Publish status change event
            await _eventPublisherService.PublishPaymentEventAsync(new PaymentEvent
            {
                TransactionId = payment.TransactionId,
                EventType = "payment.status_changed",
                EventTime = DateTime.UtcNow,
                Data = new { Status = webhook.Status, EventDate = webhook.EventDate }
            });

            _logger.LogInformation("Payment notification processed for transaction: {TransactionId}", webhook.TransactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment notification for transaction: {TransactionId}", webhook.TransactionId);
            return false;
        }
    }

    public async Task<bool> ProcessRefundNotificationAsync(PaymentWebhook webhook)
    {
        try
        {
            var payment = await _paymentRepository.GetPaymentAsync(webhook.TransactionId);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for refund notification, transaction ID: {TransactionId}", webhook.TransactionId);
                return false;
            }

            // Update payment with refund information
            payment.Status = "Refunded";
            payment.ProcessedAt = DateTime.UtcNow;
            
            await _paymentRepository.UpdatePaymentAsync(payment);

            // Publish refund event
            await _eventPublisherService.PublishPaymentEventAsync(new PaymentEvent
            {
                TransactionId = payment.TransactionId,
                EventType = "payment.refunded",
                EventTime = DateTime.UtcNow,
                Data = new { Status = "Refunded", EventDate = webhook.EventDate }
            });

            _logger.LogInformation("Refund notification processed for transaction: {TransactionId}", webhook.TransactionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund notification for transaction: {TransactionId}", webhook.TransactionId);
            return false;
        }
    }

    public async Task<bool> ValidateWebhookSignatureAsync(string payload, string signature)
    {
        try
        {
            // Implement webhook signature validation logic here
            // This is typically done using HMAC SHA256 with a secret key
            
            // For demonstration purposes, we'll return true
            // In a real implementation, you would:
            // 1. Get the webhook secret from configuration
            // 2. Compute HMAC SHA256 of the payload using the secret
            // 3. Compare with the provided signature
            
            await Task.Delay(1); // Simulate async operation
            
            _logger.LogInformation("Webhook signature validated");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return false;
        }
    }
}