using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using PaymentProcessingAPI.Configurations;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace PaymentProcessingAPI.Services;

public class EventPublisherService : IEventPublisherService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly EventGridPublisherClient _eventGridClient;
    private readonly AzureServiceBusOptions _serviceBusOptions;
    private readonly AzureEventGridOptions _eventGridOptions;
    private readonly ILogger<EventPublisherService> _logger;

    public EventPublisherService(
        ServiceBusClient serviceBusClient,
        EventGridPublisherClient eventGridClient,
        IOptions<AzureServiceBusOptions> serviceBusOptions,
        IOptions<AzureEventGridOptions> eventGridOptions,
        ILogger<EventPublisherService> logger)
    {
        _serviceBusClient = serviceBusClient;
        _eventGridClient = eventGridClient;
        _serviceBusOptions = serviceBusOptions.Value;
        _eventGridOptions = eventGridOptions.Value;
        _logger = logger;
    }

    public async Task PublishPaymentEventAsync(PaymentEvent paymentEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Determine destination based on event type
            switch (paymentEvent.EventType)
            {
                case PaymentEventTypes.PaymentProcessed:
                    await PublishToEventGridAsync(paymentEvent.EventType, paymentEvent, cancellationToken);
                    break;
                
                case PaymentEventTypes.PaymentFailed:
                case PaymentEventTypes.PaymentRefunded:
                    await PublishToServiceBusAsync(_serviceBusOptions.PaymentQueue, paymentEvent, cancellationToken);
                    break;
                
                case PaymentEventTypes.HighValueTransaction:
                    // Publish to both Event Grid and Service Bus for high-value transactions
                    await PublishToEventGridAsync(paymentEvent.EventType, paymentEvent, cancellationToken);
                    await PublishToServiceBusAsync(_serviceBusOptions.NotificationQueue, paymentEvent, cancellationToken);
                    break;
                
                default:
                    await PublishToServiceBusAsync(_serviceBusOptions.PaymentQueue, paymentEvent, cancellationToken);
                    break;
            }

            _logger.LogInformation("Payment event published successfully: {EventType} for transaction {TransactionId}", 
                paymentEvent.EventType, paymentEvent.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing payment event: {EventType} for transaction {TransactionId}", 
                paymentEvent.EventType, paymentEvent.TransactionId);
            throw;
        }
    }

    public async Task PublishToServiceBusAsync(string queueName, object message, CancellationToken cancellationToken = default)
    {
        try
        {
            var sender = _serviceBusClient.CreateSender(queueName);
            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                MessageId = Guid.NewGuid().ToString()
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            
            _logger.LogInformation("Message sent to Service Bus queue: {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Service Bus queue: {QueueName}", queueName);
            throw;
        }
    }

    public async Task PublishToEventGridAsync(string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            var eventGridEvent = new EventGridEvent(
                subject: $"payment/{eventType}",
                eventType: eventType,
                dataVersion: "1.0",
                data: BinaryData.FromObjectAsJson(eventData))
            {
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow
            };

            await _eventGridClient.SendEventAsync(eventGridEvent, cancellationToken);
            
            _logger.LogInformation("Event sent to Event Grid: {EventType}", eventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending event to Event Grid: {EventType}", eventType);
            throw;
        }
    }
}