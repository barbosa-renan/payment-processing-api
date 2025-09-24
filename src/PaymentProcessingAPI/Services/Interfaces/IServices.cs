using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;

namespace PaymentProcessingAPI.Services.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> GetPaymentStatusAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<PaymentResponse> CancelPaymentAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<PaymentResponse> RefundPaymentAsync(string transactionId, RefundRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<PaymentResponse>> GetPaymentsAsync(PaymentFilter filter, CancellationToken cancellationToken = default);
}

public interface IPaymentGatewayService
{
    Task<PaymentResponse> ProcessCreditCardAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> ProcessPixAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> ProcessBoletoAsync(PaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentResponse> ProcessDebitAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}

public interface IEventPublisherService
{
    Task PublishPaymentEventAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default);
    Task PublishToServiceBusAsync(string queueName, object message, CancellationToken cancellationToken = default);
    Task PublishToEventGridAsync(string eventType, object eventData, CancellationToken cancellationToken = default);
}

public interface IPaymentRepository
{
    Task<Payment> CreatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> GetPaymentAsync(string transactionId, CancellationToken cancellationToken = default);
    Task<Payment> UpdatePaymentAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<IEnumerable<Payment>> GetPaymentsByFilterAsync(PaymentFilter filter, CancellationToken cancellationToken = default);
    Task<int> GetPaymentsCountByFilterAsync(PaymentFilter filter, CancellationToken cancellationToken = default);
}

public interface IPaymentValidationService
{
    bool ValidateCreditCard(string cardNumber);
    bool ValidateDocument(string document);
    bool ValidateTransactionDuplication(string transactionId);
    Task<bool> ValidateRateLimitAsync(string customerId, CancellationToken cancellationToken = default);
}