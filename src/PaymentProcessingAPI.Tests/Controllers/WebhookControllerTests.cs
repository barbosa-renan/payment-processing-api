using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentProcessingAPI.Controllers;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Tests.Controllers;

public class WebhookControllerTests
{
    private readonly Mock<IWebhookService> _mockWebhookService;
    private readonly Mock<ILogger<WebhookController>> _mockLogger;
    private readonly WebhookController _controller;

    public WebhookControllerTests()
    {
        _mockWebhookService = new Mock<IWebhookService>();
        _mockLogger = new Mock<ILogger<WebhookController>>();
        _controller = new WebhookController(_mockWebhookService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ReceiveWebhook_ValidPayload_ReturnsOkResult()
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Approved"
        };

        _mockWebhookService.Setup(s => s.ProcessWebhookAsync(It.IsAny<PaymentWebhook>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ReceiveWebhook(webhook);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<OkResult>();
        _mockWebhookService.Verify(s => s.ProcessWebhookAsync(webhook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveWebhook_InvalidPayload_ReturnsBadRequestResult()
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = PaymentStatus.Failed,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Declined"
        };

        _mockWebhookService.Setup(s => s.ProcessWebhookAsync(It.IsAny<PaymentWebhook>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.ReceiveWebhook(webhook);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task ReceiveWebhook_ServiceException_ReturnsInternalServerError()
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Approved"
        };

        _mockWebhookService.Setup(s => s.ProcessWebhookAsync(It.IsAny<PaymentWebhook>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _controller.ReceiveWebhook(webhook));
    }

    [Fact]
    public async Task ReceiveWebhook_NullPayload_ReturnsBadRequestResult()
    {
        // Act
        var result = await _controller.ReceiveWebhook(null);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestResult>();
        _mockWebhookService.Verify(s => s.ProcessWebhookAsync(It.IsAny<PaymentWebhook>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ReceiveWebhook_InvalidTransactionId_ReturnsBadRequestResult(string transactionId)
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Approved"
        };

        // Act
        var result = await _controller.ReceiveWebhook(webhook);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<BadRequestResult>();
        _mockWebhookService.Verify(s => s.ProcessWebhookAsync(It.IsAny<PaymentWebhook>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReceiveWebhook_DuplicateWebhook_ReturnsOkResult()
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Approved"
        };

        // Simulating idempotent behavior - duplicate webhooks should return OK but not process again
        _mockWebhookService.Setup(s => s.ProcessWebhookAsync(It.IsAny<PaymentWebhook>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act - Send same webhook twice
        var result1 = await _controller.ReceiveWebhook(webhook);
        var result2 = await _controller.ReceiveWebhook(webhook);

        // Assert
        result1.Should().BeOfType<OkResult>();
        result2.Should().BeOfType<OkResult>();
        _mockWebhookService.Verify(s => s.ProcessWebhookAsync(webhook, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ReceiveWebhook_ProcessingTimeout_HandlesGracefully()
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = Guid.NewGuid().ToString(),
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Approved"
        };

        _mockWebhookService.Setup(s => s.ProcessWebhookAsync(It.IsAny<PaymentWebhook>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _controller.ReceiveWebhook(webhook));
    }
}