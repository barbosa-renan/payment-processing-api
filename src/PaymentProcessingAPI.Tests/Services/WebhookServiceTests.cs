using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;
using PaymentProcessingAPI.Services;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Tests.Services;

public class WebhookServiceTests
{
    private readonly Mock<IPaymentRepository> _mockRepository;
    private readonly Mock<IEventPublisherService> _mockEventPublisher;
    private readonly Mock<ILogger<WebhookService>> _mockLogger;
    private readonly WebhookService _service;

    public WebhookServiceTests()
    {
        _mockRepository = new Mock<IPaymentRepository>();
        _mockEventPublisher = new Mock<IEventPublisherService>();
        _mockLogger = new Mock<ILogger<WebhookService>>();

        _service = new WebhookService(
            _mockRepository.Object,
            _mockEventPublisher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessWebhookAsync_ValidWebhook_ProcessesSuccessfully()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        var existingPayment = CreateTestPayment();
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);
        
        _mockRepository.Setup(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockEventPublisher.Verify(e => e.PublishPaymentEventAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWebhookAsync_PaymentNotFound_ReturnsFalse()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment?)null);

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockEventPublisher.Verify(e => e.PublishPaymentEventAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhookAsync_DuplicateWebhook_ProcessesIdempotently()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        var existingPayment = CreateTestPayment();
        existingPayment.Status = PaymentStatus.Approved; // Already processed
        existingPayment.GatewayTransactionId = webhook.GatewayTransactionId; // Same gateway transaction ID
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()), Times.Once);
        // Should not update if already processed
        _mockRepository.Verify(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhookAsync_PaymentStatusUpdate_UpdatesCorrectly()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        webhook.Status = PaymentStatus.Declined;
        webhook.GatewayResponse = "Insufficient funds";
        
        var existingPayment = CreateTestPayment();
        existingPayment.Status = PaymentStatus.Pending;
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);
        
        _mockRepository.Setup(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdatePaymentAsync(
            It.Is<Payment>(p => 
                p.Status == PaymentStatus.Declined &&
                p.GatewayResponse == "Insufficient funds" &&
                p.GatewayTransactionId == webhook.GatewayTransactionId), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWebhookAsync_RefundWebhook_ProcessesRefund()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        webhook.Status = PaymentStatus.Refunded;
        webhook.Amount = 50.00m; // Partial refund
        
        var existingPayment = CreateTestPayment();
        existingPayment.Status = PaymentStatus.Approved;
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);
        
        _mockRepository.Setup(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdatePaymentAsync(
            It.Is<Payment>(p => 
                p.Status == PaymentStatus.Refunded &&
                p.RefundedAmount == webhook.Amount), 
            It.IsAny<CancellationToken>()), Times.Once);
        
        _mockEventPublisher.Verify(e => e.PublishPaymentEventAsync(
            It.Is<PaymentEvent>(pe => pe.EventType == PaymentEventTypes.PaymentRefunded), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessWebhookAsync_NullWebhook_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.ProcessWebhookAsync(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ProcessWebhookAsync_InvalidTransactionId_ReturnsFalse(string transactionId)
    {
        // Arrange
        var webhook = CreateValidWebhook();
        webhook.TransactionId = transactionId;

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.GetPaymentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessWebhookAsync_RepositoryThrowsException_HandlesGracefully()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessWebhookAsync_EventPublisherThrowsException_StillProcessesWebhook()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        var existingPayment = CreateTestPayment();
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);
        
        _mockRepository.Setup(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        _mockEventPublisher.Setup(e => e.PublishPaymentEventAsync(It.IsAny<PaymentEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Event publishing failed"));

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeTrue(); // Should still return true as payment was updated
        _mockRepository.Verify(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessWebhookAsync_InvalidAmount_DoesNotUpdateAmount()
    {
        // Arrange
        var webhook = CreateValidWebhook();
        webhook.Amount = -10.00m; // Invalid negative amount
        
        var existingPayment = CreateTestPayment();
        
        _mockRepository.Setup(r => r.GetPaymentAsync(webhook.TransactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPayment);
        
        _mockRepository.Setup(r => r.UpdatePaymentAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Payment p, CancellationToken ct) => p);

        // Act
        var result = await _service.ProcessWebhookAsync(webhook);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.UpdatePaymentAsync(
            It.Is<Payment>(p => p.Amount == existingPayment.Amount), // Original amount should be preserved
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static PaymentWebhook CreateValidWebhook()
    {
        return new PaymentWebhook
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Approved"
        };
    }

    private static Payment CreateTestPayment()
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.CreditCard,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
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
            }
        };
    }
}