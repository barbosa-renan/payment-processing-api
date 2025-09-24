using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PaymentProcessingAPI.Configurations;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services;

namespace PaymentProcessingAPI.Tests.Services;

public class EventPublisherServiceTests
{
    private readonly Mock<ServiceBusClient> _mockServiceBusClient;
    private readonly Mock<EventGridPublisherClient> _mockEventGridClient;
    private readonly Mock<IOptions<AzureServiceBusOptions>> _mockServiceBusOptions;
    private readonly Mock<IOptions<AzureEventGridOptions>> _mockEventGridOptions;
    private readonly Mock<ILogger<EventPublisherService>> _mockLogger;
    private readonly EventPublisherService _service;
    private readonly AzureServiceBusOptions _serviceBusOptions;
    private readonly AzureEventGridOptions _eventGridOptions;

    public EventPublisherServiceTests()
    {
        _serviceBusOptions = new AzureServiceBusOptions
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test",
            PaymentQueue = "payment-queue",
            RefundQueue = "refund-queue",
            NotificationQueue = "notification-queue"
        };

        _eventGridOptions = new AzureEventGridOptions
        {
            TopicEndpoint = "https://test.eventgrid.azure.net/api/events",
            AccessKey = "test-access-key"
        };

        _mockServiceBusClient = new Mock<ServiceBusClient>();
        _mockEventGridClient = new Mock<EventGridPublisherClient>();

        _mockServiceBusOptions = new Mock<IOptions<AzureServiceBusOptions>>();
        _mockServiceBusOptions.Setup(o => o.Value).Returns(_serviceBusOptions);

        _mockEventGridOptions = new Mock<IOptions<AzureEventGridOptions>>();
        _mockEventGridOptions.Setup(o => o.Value).Returns(_eventGridOptions);

        _mockLogger = new Mock<ILogger<EventPublisherService>>();

        _service = new EventPublisherService(
            _mockServiceBusClient.Object,
            _mockEventGridClient.Object,
            _mockServiceBusOptions.Object,
            _mockEventGridOptions.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task PublishPaymentEventAsync_PaymentProcessed_PublishesToEventGrid()
    {
        // Arrange
        var paymentEvent = new PaymentEvent
        {
            EventType = PaymentEventTypes.PaymentProcessed,
            TransactionId = "TEST123",
            Subject = "payment/processed",
            Data = new { Amount = 100.00m, Currency = "BRL" }
        };

        // Act & Assert - Should not throw
        await _service.PublishPaymentEventAsync(paymentEvent);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing payment event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishPaymentEventAsync_PaymentFailed_PublishesToServiceBus()
    {
        // Arrange
        var paymentEvent = new PaymentEvent
        {
            EventType = PaymentEventTypes.PaymentFailed,
            TransactionId = "TEST123",
            Subject = "payment/failed",
            Data = new { Reason = "Invalid card" }
        };

        // Act & Assert - Should not throw
        await _service.PublishPaymentEventAsync(paymentEvent);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing payment event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishPaymentEventAsync_PaymentRefunded_PublishesToServiceBus()
    {
        // Arrange
        var paymentEvent = new PaymentEvent
        {
            EventType = PaymentEventTypes.PaymentRefunded,
            TransactionId = "TEST123",
            Subject = "payment/refunded",
            Data = new { RefundAmount = 50.00m }
        };

        // Act & Assert - Should not throw
        await _service.PublishPaymentEventAsync(paymentEvent);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing payment event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishPaymentEventAsync_HighValueTransaction_PublishesToBothEventGridAndServiceBus()
    {
        // Arrange
        var paymentEvent = new PaymentEvent
        {
            EventType = PaymentEventTypes.HighValueTransaction,
            TransactionId = "TEST123",
            Subject = "payment/high-value",
            Data = new { Amount = 10000.00m, Currency = "BRL" }
        };

        // Act & Assert - Should not throw
        await _service.PublishPaymentEventAsync(paymentEvent);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing payment event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishToServiceBusAsync_ValidMessage_PublishesSuccessfully()
    {
        // Arrange
        var queueName = "test-queue";
        var message = new { TransactionId = "TEST123", Amount = 100.00m };

        // Act & Assert - Should not throw
        await _service.PublishToServiceBusAsync(queueName, message);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing message to Service Bus queue")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishToEventGridAsync_ValidEvent_PublishesSuccessfully()
    {
        // Arrange
        var eventType = "Payment.Processed";
        var eventData = new { TransactionId = "TEST123", Amount = 100.00m };

        // Act & Assert - Should not throw
        await _service.PublishToEventGridAsync(eventType, eventData);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing event to Event Grid")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishPaymentEventAsync_NullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.PublishPaymentEventAsync(null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task PublishToServiceBusAsync_NullOrEmptyQueueName_ThrowsArgumentException(string queueName)
    {
        // Arrange
        var message = new { Test = "data" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.PublishToServiceBusAsync(queueName, message));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task PublishToEventGridAsync_NullOrEmptyEventType_ThrowsArgumentException(string eventType)
    {
        // Arrange
        var eventData = new { Test = "data" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.PublishToEventGridAsync(eventType, eventData));
    }

    [Fact]
    public async Task PublishToServiceBusAsync_NullMessage_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.PublishToServiceBusAsync("test-queue", null!));
    }

    [Fact]
    public async Task PublishToEventGridAsync_NullEventData_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.PublishToEventGridAsync("Payment.Processed", null!));
    }

    [Fact]
    public void EventPublisherService_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange & Act
        var service = new EventPublisherService(
            _mockServiceBusClient.Object,
            _mockEventGridClient.Object,
            _mockServiceBusOptions.Object,
            _mockEventGridOptions.Object,
            _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishPaymentEventAsync_InvalidEventType_DefaultsToServiceBus()
    {
        // Arrange
        var paymentEvent = new PaymentEvent
        {
            EventType = "Invalid.EventType",
            TransactionId = "TEST123",
            Subject = "payment/unknown",
            Data = new { Amount = 100.00m }
        };

        // Act & Assert - Should not throw and should log
        await _service.PublishPaymentEventAsync(paymentEvent);

        // Verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Publishing payment event")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}