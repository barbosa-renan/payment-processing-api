using AutoMapper;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;
using PaymentProcessingAPI.Services.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PaymentProcessingAPI.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGatewayService _paymentGatewayService;
    private readonly IEventPublisherService _eventPublisherService;
    private readonly IPaymentValidationService _validationService;
    private readonly IMapper _mapper;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IPaymentGatewayService paymentGatewayService,
        IEventPublisherService eventPublisherService,
        IPaymentValidationService validationService,
        IMapper mapper,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _paymentGatewayService = paymentGatewayService;
        _eventPublisherService = eventPublisherService;
        _validationService = validationService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting payment processing for transaction {TransactionId}", request.TransactionId);

            // Validate rate limiting
            if (!await _validationService.ValidateRateLimitAsync(request.Customer.CustomerId, cancellationToken))
            {
                _logger.LogWarning("Rate limit exceeded for customer {CustomerId}", request.Customer.CustomerId);
                return CreateErrorResponse(request.TransactionId, "Rate limit exceeded. Please try again later.");
            }

            // Validate business rules
            var validationResult = ValidatePaymentRequest(request);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Payment validation failed for transaction {TransactionId}: {Errors}", 
                    request.TransactionId, string.Join(", ", validationResult.Errors));
                return CreateErrorResponse(request.TransactionId, string.Join(", ", validationResult.Errors));
            }

            // Check for transaction duplication
            var existingPayment = await _paymentRepository.GetPaymentAsync(request.TransactionId, cancellationToken);
            if (existingPayment != null)
            {
                _logger.LogWarning("Duplicate transaction detected: {TransactionId}", request.TransactionId);
                return CreateErrorResponse(request.TransactionId, "Duplicate transaction ID");
            }

            // Create payment record
            var payment = CreatePaymentEntity(request);
            payment = await _paymentRepository.CreatePaymentAsync(payment, cancellationToken);

            // Process payment with gateway
            var gatewayResponse = await ProcessWithGateway(request, cancellationToken);

            // Update payment with response
            payment = UpdatePaymentWithResponse(payment, gatewayResponse);
            await _paymentRepository.UpdatePaymentAsync(payment, cancellationToken);

            // Publish events
            await PublishPaymentEvents(payment, gatewayResponse, cancellationToken);

            _logger.LogInformation("Payment processing completed for transaction {TransactionId} with status {Status}", 
                request.TransactionId, gatewayResponse.Status);

            return gatewayResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for transaction {TransactionId}", request.TransactionId);
            
            return CreateErrorResponse(request.TransactionId, "Payment processing failed");
        }
    }

    public async Task<PaymentResponse> GetPaymentStatusAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetPaymentAsync(transactionId, cancellationToken);
            
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for transaction {TransactionId}", transactionId);
                return CreateErrorResponse(transactionId, "Payment not found");
            }

            return _mapper.Map<PaymentResponse>(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment status for transaction {TransactionId}", transactionId);
            return CreateErrorResponse(transactionId, "Error retrieving payment status");
        }
    }

    public async Task<PaymentResponse> CancelPaymentAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetPaymentAsync(transactionId, cancellationToken);
            
            if (payment == null)
            {
                return CreateErrorResponse(transactionId, "Payment not found");
            }

            if (payment.Status != PaymentStatus.Pending.ToString() && payment.Status != PaymentStatus.Processing.ToString())
            {
                return CreateErrorResponse(transactionId, $"Cannot cancel payment with status {payment.Status}");
            }

            payment.Status = PaymentStatus.Cancelled.ToString();
            payment.Message = "Payment cancelled by user";
            await _paymentRepository.UpdatePaymentAsync(payment, cancellationToken);

            // Publish cancellation event
            var paymentEvent = new PaymentEvent
            {
                EventType = PaymentEventTypes.PaymentCancelled,
                TransactionId = transactionId,
                Data = new { Status = PaymentStatus.Cancelled, Reason = "User cancellation" }
            };

            await _eventPublisherService.PublishPaymentEventAsync(paymentEvent, cancellationToken);

            _logger.LogInformation("Payment cancelled for transaction {TransactionId}", transactionId);

            return _mapper.Map<PaymentResponse>(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment for transaction {TransactionId}", transactionId);
            return CreateErrorResponse(transactionId, "Error cancelling payment");
        }
    }

    public async Task<PaymentResponse> RefundPaymentAsync(string transactionId, RefundRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _paymentRepository.GetPaymentAsync(transactionId, cancellationToken);
            
            if (payment == null)
            {
                return CreateErrorResponse(transactionId, "Payment not found");
            }

            if (payment.Status != PaymentStatus.Approved.ToString())
            {
                return CreateErrorResponse(transactionId, $"Cannot refund payment with status {payment.Status}");
            }

            if (request.Amount > payment.ProcessedAmount)
            {
                return CreateErrorResponse(transactionId, "Refund amount cannot exceed processed amount");
            }

            payment.Status = PaymentStatus.Refunded.ToString();
            payment.Message = $"Refund processed: {request.Reason ?? "No reason provided"}";
            await _paymentRepository.UpdatePaymentAsync(payment, cancellationToken);

            // Publish refund event
            var paymentEvent = new PaymentEvent
            {
                EventType = PaymentEventTypes.PaymentRefunded,
                TransactionId = transactionId,
                Data = new { RefundAmount = request.Amount, Reason = request.Reason }
            };

            await _eventPublisherService.PublishPaymentEventAsync(paymentEvent, cancellationToken);

            _logger.LogInformation("Payment refunded for transaction {TransactionId}, amount: {Amount}", 
                transactionId, request.Amount);

            return _mapper.Map<PaymentResponse>(payment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return CreateErrorResponse(transactionId, "Error processing refund");
        }
    }

    public async Task<PagedResult<PaymentResponse>> GetPaymentsAsync(PaymentFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _paymentRepository.GetPaymentsByFilterAsync(filter, cancellationToken);
            var totalCount = await _paymentRepository.GetPaymentsCountByFilterAsync(filter, cancellationToken);

            var paymentResponses = _mapper.Map<IEnumerable<PaymentResponse>>(payments);

            return new PagedResult<PaymentResponse>
            {
                Items = paymentResponses,
                TotalItems = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments with filter");
            return new PagedResult<PaymentResponse>();
        }
    }

    private ValidationResult ValidatePaymentRequest(PaymentRequest request)
    {
        var errors = new List<string>();

        // Validate document
        if (!_validationService.ValidateDocument(request.Customer.Document))
        {
            errors.Add("Invalid customer document");
        }

        // Validate card for card payments
        if ((request.PaymentMethod == PaymentMethod.CreditCard || request.PaymentMethod == PaymentMethod.Debit) && request.Card != null)
        {
            if (!_validationService.ValidateCreditCard(request.Card.Number))
            {
                errors.Add("Invalid card number");
            }
        }

        // Validate transaction duplication
        if (!_validationService.ValidateTransactionDuplication(request.TransactionId))
        {
            errors.Add("Invalid transaction ID format");
        }

        return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    private async Task<PaymentResponse> ProcessWithGateway(PaymentRequest request, CancellationToken cancellationToken)
    {
        return request.PaymentMethod switch
        {
            PaymentMethod.CreditCard => await _paymentGatewayService.ProcessCreditCardAsync(request, cancellationToken),
            PaymentMethod.Debit => await _paymentGatewayService.ProcessDebitAsync(request, cancellationToken),
            PaymentMethod.Pix => await _paymentGatewayService.ProcessPixAsync(request, cancellationToken),
            PaymentMethod.Boleto => await _paymentGatewayService.ProcessBoletoAsync(request, cancellationToken),
            _ => throw new ArgumentException($"Unsupported payment method: {request.PaymentMethod}")
        };
    }

    private Payment CreatePaymentEntity(PaymentRequest request)
    {
        return new Payment
        {
            TransactionId = request.TransactionId,
            Amount = request.Amount,
            Currency = request.Currency.ToString(),
            PaymentMethod = request.PaymentMethod.ToString(),
            Status = PaymentStatus.Processing.ToString(),
            CustomerId = request.Customer.CustomerId,
            CustomerName = request.Customer.Name,
            CustomerEmail = request.Customer.Email,
            CustomerDocument = request.Customer.Document,
            AddressStreet = request.Customer.Address.Street,
            AddressNumber = request.Customer.Address.Number,
            AddressComplement = request.Customer.Address.Complement,
            AddressNeighborhood = request.Customer.Address.Neighborhood,
            AddressCity = request.Customer.Address.City,
            AddressState = request.Customer.Address.State,
            AddressZipCode = request.Customer.Address.ZipCode,
            AddressCountry = request.Customer.Address.Country,
            CardNumberMasked = request.Card != null ? MaskCardNumber(request.Card.Number) : null,
            CardHolderName = request.Card?.HolderName,
            CardBrand = request.Card?.Brand,
            MetadataJson = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null
        };
    }

    private static Payment UpdatePaymentWithResponse(Payment payment, PaymentResponse response)
    {
        payment.Status = response.Status.ToString();
        payment.AuthorizationCode = response.AuthorizationCode;
        payment.ProcessedAt = response.ProcessedAt;
        payment.Message = response.Message;
        payment.ProcessedAmount = response.ProcessedAmount;

        if (response.Fees != null)
        {
            payment.ProcessingFee = response.Fees.ProcessingFee;
            payment.GatewayFee = response.Fees.GatewayFee;
            payment.TotalFees = response.Fees.TotalFees;
            payment.NetAmount = response.Fees.NetAmount;
        }

        return payment;
    }

    private async Task PublishPaymentEvents(Payment payment, PaymentResponse response, CancellationToken cancellationToken)
    {
        var eventType = response.Status switch
        {
            PaymentStatus.Approved => PaymentEventTypes.PaymentProcessed,
            PaymentStatus.Declined or PaymentStatus.Failed => PaymentEventTypes.PaymentFailed,
            _ => PaymentEventTypes.PaymentProcessed
        };

        // Check for high-value transaction
        if (payment.Amount >= 10000) // High value threshold
        {
            eventType = PaymentEventTypes.HighValueTransaction;
        }

        var paymentEvent = new PaymentEvent
        {
            EventType = eventType,
            TransactionId = payment.TransactionId,
            Data = new
            {
                Amount = payment.Amount,
                Status = response.Status,
                PaymentMethod = payment.PaymentMethod,
                CustomerId = payment.CustomerId
            }
        };

        await _eventPublisherService.PublishPaymentEventAsync(paymentEvent, cancellationToken);
    }

    private static string MaskCardNumber(string cardNumber)
    {
        if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 8)
            return cardNumber;

        var cleaned = Regex.Replace(cardNumber, @"[\s-]", "");
        return $"{cleaned[..4]}****{cleaned[^4..]}";
    }

    private static PaymentResponse CreateErrorResponse(string transactionId, string message)
    {
        return new PaymentResponse
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            Message = message,
            ProcessedAmount = 0
        };
    }

    private class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}