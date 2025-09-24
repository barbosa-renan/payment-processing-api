using PaymentProcessingAPI.Models;

namespace PaymentProcessingAPI.Services.Interfaces;

public interface IWebhookService
{
    Task<bool> ProcessPaymentNotificationAsync(PaymentWebhook webhook);
    Task<bool> ProcessRefundNotificationAsync(PaymentWebhook webhook);
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature);
}