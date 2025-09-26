// Exemplo de uso do ServiceBusService
// Este arquivo serve apenas como documentação

using PaymentProcessingAPI.Models.ServiceBus;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Examples
{
    public class ServiceBusExamples
    {
        private readonly IServiceBusService _serviceBusService;

        public ServiceBusExamples(IServiceBusService serviceBusService)
        {
            _serviceBusService = serviceBusService;
        }

        public async Task ExampleUsage()
        {
            // Exemplo 1: Enviar mensagem de pagamento processado
            var paymentProcessedMessage = new PaymentProcessedMessage
            {
                TransactionId = "TXN-123456",
                CustomerId = "CUST-789",
                CustomerEmail = "customer@example.com",
                Amount = 100.50m,
                Currency = "BRL",
                PaymentMethod = "CREDIT_CARD",
                AuthorizationCode = "AUTH-ABC123",
                ProcessedAt = DateTime.UtcNow,
                OrderId = "ORD-456789",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = "PROD-001",
                        ProductName = "Produto Exemplo",
                        Quantity = 2,
                        UnitPrice = 50.25m
                    }
                }
            };

            await _serviceBusService.SendPaymentProcessedAsync(paymentProcessedMessage);

            // Exemplo 2: Enviar mensagem de pagamento falhado
            var paymentFailedMessage = new PaymentFailedMessage
            {
                TransactionId = "TXN-123457",
                CustomerId = "CUST-790",
                Amount = 75.00m,
                Currency = "BRL",
                PaymentMethod = "DEBIT_CARD",
                FailureReason = "Insufficient funds",
                ErrorCode = "ERR-001",
                FailedAt = DateTime.UtcNow,
                IsRetryable = true
            };

            await _serviceBusService.SendPaymentFailedAsync(paymentFailedMessage);

            // Exemplo 3: Enviar notificação
            var notificationMessage = new NotificationMessage
            {
                CustomerId = "CUST-789",
                TransactionId = "TXN-123456",
                NotificationType = "PAYMENT_SUCCESS",
                Channel = "EMAIL",
                Data = new Dictionary<string, object>
                {
                    { "customerName", "João Silva" },
                    { "amount", 100.50m },
                    { "currency", "BRL" }
                },
                CreatedAt = DateTime.UtcNow
            };

            await _serviceBusService.SendNotificationAsync(notificationMessage);

            // Exemplo 4: Enviar mensagem genérica
            await _serviceBusService.SendMessageAsync("custom-queue", paymentProcessedMessage);

            // Exemplo 5: Enviar lote de mensagens
            var messages = new List<PaymentProcessedMessage> { paymentProcessedMessage };
            await _serviceBusService.SendBatchMessagesAsync("payment-processed-queue", messages);
        }
    }
}