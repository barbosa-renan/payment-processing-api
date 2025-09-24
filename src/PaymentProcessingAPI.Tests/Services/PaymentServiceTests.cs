using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Tests.Services;

public class PaymentServiceTests
{
    private readonly Mock<IPaymentRepository> _mockRepository;
    private readonly Mock<IPaymentGatewayService> _mockGatewayService;
    private readonly Mock<IEventPublisherService> _mockEventPublisher;
    private readonly Mock<IPaymentValidationService> _mockValidationService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PaymentService>> _mockLogger;
    private readonly PaymentService _paymentService;

    public PaymentServiceTests()
    {
        _mockRepository = new Mock<IPaymentRepository>();
        _mockGatewayService = new Mock<IPaymentGatewayService>();
        _mockEventPublisher = new Mock<IEventPublisherService>();
        _mockValidationService = new Mock<IPaymentValidationService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PaymentService>>();

        _paymentService = new PaymentService(
            _mockRepository.Object,
            _mockGatewayService.Object,
            _mockEventPublisher.Object,
            _mockValidationService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ValidRequest_ReturnsSuccessfulResponse()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var gatewayResponse = CreateSuccessfulGatewayResponse();

        _mockValidationService.Setup(v => v.ValidateRateLimitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockValidationService.Setup(v => v.ValidateDocument(It.IsAny<string>()))
            .Returns(true);

        _mockValidationService.Setup(v => v.ValidateCreditCard(It.IsAny<string>()))
            .Returns(true);

        _mockValidationService.Setup(v => v.ValidateTransactionDuplication(It.IsAny<string>()))
            .Returns(true);

        _mockRepository.Setup(r => r.GetPaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Models.Entities.Payment?)null);

        _mockRepository.Setup(r => r.CreatePaymentAsync(It.IsAny<Models.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Models.Entities.Payment payment) => payment);

        _mockRepository.Setup(r => r.UpdatePaymentAsync(It.IsAny<Models.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Models.Entities.Payment payment) => payment);

        _mockGatewayService.Setup(g => g.ProcessCreditCardAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gatewayResponse);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Approved);
        result.TransactionId.Should().Be(request.TransactionId);

        _mockRepository.Verify(r => r.CreatePaymentAsync(It.IsAny<Models.Entities.Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdatePaymentAsync(It.IsAny<Models.Entities.Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishPaymentEventAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_RateLimitExceeded_ReturnsFailedResponse()
    {
        // Arrange
        var request = CreateValidPaymentRequest();

        _mockValidationService.Setup(v => v.ValidateRateLimitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Failed);
        result.Message.Should().Contain("Rate limit exceeded");

        _mockRepository.Verify(r => r.CreatePaymentAsync(It.IsAny<Models.Entities.Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_DuplicateTransaction_ReturnsFailedResponse()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var existingPayment = new Models.Entities.Payment { TransactionId = request.TransactionId };

        _mockValidationService.Setup(v => v.ValidateRateLimitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockValidationService.Setup(v => v.ValidateDocument(It.IsAny<string>()))
            .Returns(true);

        _mockValidationService.Setup(v => v.ValidateCreditCard(It.IsAny<string>()))
            .Returns(true);

        _mockValidationService.Setup(v => v.ValidateTransactionDuplication(It.IsAny<string>()))
            .Returns(true);

        _mockRepository.Setup(r => r.GetPaymentAsync(request.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _paymentService.ProcessPaymentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Failed);
        result.Message.Should().Contain("Duplicate transaction ID");
    }

    [Fact]
    public async Task GetPaymentStatusAsync_ExistingPayment_ReturnsPaymentResponse()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var payment = new Models.Entities.Payment { TransactionId = transactionId };
        var expectedResponse = new PaymentResponse { TransactionId = transactionId };

        _mockRepository.Setup(r => r.GetPaymentAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _mockMapper.Setup(m => m.Map<PaymentResponse>(payment))
            .Returns(expectedResponse);

        // Act
        var result = await _paymentService.GetPaymentStatusAsync(transactionId);

        // Assert
        result.Should().NotBeNull();
        result.TransactionId.Should().Be(transactionId);

        _mockRepository.Verify(r => r.GetPaymentAsync(transactionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPaymentStatusAsync_NonExistentPayment_ReturnsNotFoundResponse()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();

        _mockRepository.Setup(r => r.GetPaymentAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Models.Entities.Payment?)null);

        // Act
        var result = await _paymentService.GetPaymentStatusAsync(transactionId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Failed);
        result.Message.Should().Contain("Payment not found");
    }

    [Fact]
    public async Task CancelPaymentAsync_PendingPayment_ReturnsSuccessfulCancellation()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var payment = new Models.Entities.Payment
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Pending.ToString()
        };

        _mockRepository.Setup(r => r.GetPaymentAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(payment);

        _mockRepository.Setup(r => r.UpdatePaymentAsync(It.IsAny<Models.Entities.Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Models.Entities.Payment p) => p);

        _mockMapper.Setup(m => m.Map<PaymentResponse>(It.IsAny<Models.Entities.Payment>()))
            .Returns(new PaymentResponse { TransactionId = transactionId, Status = PaymentStatus.Cancelled });

        // Act
        var result = await _paymentService.CancelPaymentAsync(transactionId);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Cancelled);

        _mockRepository.Verify(r => r.UpdatePaymentAsync(It.IsAny<Models.Entities.Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishPaymentEventAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PaymentRequest CreateValidPaymentRequest()
    {
        return new PaymentRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.CreditCard,
            Customer = new CustomerInfo
            {
                CustomerId = "CUST001",
                Name = "John Doe",
                Email = "john.doe@example.com",
                Document = "12345678901",
                Address = new AddressInfo
                {
                    Street = "Main St",
                    Number = "123",
                    City = "São Paulo",
                    State = "SP",
                    ZipCode = "01234567",
                    Country = "Brazil",
                    Neighborhood = "Centro"
                }
            },
            Card = new CardInfo
            {
                Number = "4111111111111111",
                HolderName = "John Doe",
                ExpiryMonth = "12",
                ExpiryYear = "2025",
                CVV = "123",
                Brand = "VISA"
            }
        };
    }

    private static PaymentResponse CreateSuccessfulGatewayResponse()
    {
        return new PaymentResponse
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = PaymentStatus.Approved,
            AuthorizationCode = "AUTH123456",
            ProcessedAt = DateTime.UtcNow,
            Message = "Payment approved successfully",
            ProcessedAmount = 100.00m,
            Fees = new PaymentFees
            {
                ProcessingFee = 3.50m,
                GatewayFee = 0.30m,
                TotalFees = 3.80m,
                NetAmount = 96.20m
            }
        };
    }
}