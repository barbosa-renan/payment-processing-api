using PaymentProcessingAPI.Models.ServiceBus;

namespace PaymentProcessingAPI.Services.Interfaces
{
    public interface IServiceBusService
    {
        Task SendPaymentProcessedAsync(PaymentProcessedMessage message);
        Task SendPaymentFailedAsync(PaymentFailedMessage message);
        Task SendNotificationAsync(NotificationMessage message);
        Task SendRefundRequestAsync(RefundRequestMessage message);
        Task SendHighValueApprovalAsync(HighValueApprovalMessage message);
        
        // Métodos genéricos
        Task SendMessageAsync<T>(string queueName, T message) where T : class;
        Task SendBatchMessagesAsync<T>(string queueName, IEnumerable<T> messages) where T : class;
    }
}