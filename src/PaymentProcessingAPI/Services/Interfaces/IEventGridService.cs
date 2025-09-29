using PaymentProcessingAPI.Models.EventGrid;

namespace PaymentProcessingAPI.Services.Interfaces;

public interface IEventGridService
{
    Task PublishPaymentProcessedAsync(PaymentProcessedEventData eventData);
    Task PublishPaymentFailedAsync(PaymentFailedEventData eventData);
    Task PublishHighValueTransactionAsync(HighValueTransactionEventData eventData);
    Task PublishPaymentRefundedAsync(PaymentRefundedEventData eventData);
    Task PublishPaymentStatusChangedAsync(PaymentStatusChangedEventData eventData);
    
    // Métodos genéricos
    Task PublishEventAsync<T>(string eventType, string subject, T data);
    Task PublishEventsAsync<T>(List<(string eventType, string subject, T data)> events);
}