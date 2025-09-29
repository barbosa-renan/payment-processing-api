using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Options;
using PaymentProcessingAPI.Configuration;
using PaymentProcessingAPI.Constants;
using PaymentProcessingAPI.Models.EventGrid;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Services;

public class EventGridService : IEventGridService
{
    private readonly EventGridPublisherClient _client;
    private readonly EventGridConfiguration _config;
    private readonly ILogger<EventGridService> _logger;

    public EventGridService(
        IOptions<EventGridConfiguration> config,
        ILogger<EventGridService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        _client = new EventGridPublisherClient(
            new Uri(_config.Endpoint),
            new AzureKeyCredential(_config.AccessKey));
    }

    public async Task PublishPaymentProcessedAsync(PaymentProcessedEventData eventData)
    {
        _logger.LogInformation("Publishing payment processed event for transaction {TransactionId}", 
            eventData.TransactionId);

        await PublishEventAsync(
            EventTypes.PaymentProcessed,
            $"payment/{eventData.TransactionId}",
            eventData);
    }

    public async Task PublishPaymentFailedAsync(PaymentFailedEventData eventData)
    {
        _logger.LogInformation("Publishing payment failed event for transaction {TransactionId}", 
            eventData.TransactionId);

        await PublishEventAsync(
            EventTypes.PaymentFailed,
            $"payment/{eventData.TransactionId}",
            eventData);
    }

    public async Task PublishHighValueTransactionAsync(HighValueTransactionEventData eventData)
    {
        _logger.LogInformation("Publishing high value transaction event for transaction {TransactionId}", 
            eventData.TransactionId);

        await PublishEventAsync(
            EventTypes.HighValueTransaction,
            $"transaction/{eventData.TransactionId}",
            eventData);
    }

    public async Task PublishPaymentRefundedAsync(PaymentRefundedEventData eventData)
    {
        _logger.LogInformation("Publishing payment refunded event for transaction {TransactionId}", 
            eventData.TransactionId);

        await PublishEventAsync(
            EventTypes.PaymentRefunded,
            $"payment/{eventData.TransactionId}",
            eventData);
    }

    public async Task PublishPaymentStatusChangedAsync(PaymentStatusChangedEventData eventData)
    {
        _logger.LogInformation("Publishing payment status changed event for transaction {TransactionId}", 
            eventData.TransactionId);

        await PublishEventAsync(
            EventTypes.PaymentStatusChanged,
            $"payment/{eventData.TransactionId}",
            eventData);
    }

    public async Task PublishEventAsync<T>(string eventType, string subject, T data)
    {
        try
        {
            var eventGridEvent = new EventGridEvent(
                subject: subject,
                eventType: eventType,
                dataVersion: _config.DataVersion,
                data: data)
            {
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow
            };

            await _client.SendEventAsync(eventGridEvent);

            _logger.LogInformation(
                "Event published successfully. EventType: {EventType}, Subject: {Subject}, EventId: {EventId}",
                eventType, subject, eventGridEvent.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to publish event. EventType: {EventType}, Subject: {Subject}", 
                eventType, subject);
            throw;
        }
    }

    public async Task PublishEventsAsync<T>(List<(string eventType, string subject, T data)> events)
    {
        try
        {
            var eventGridEvents = events.Select(e => new EventGridEvent(
                subject: e.subject,
                eventType: e.eventType,
                dataVersion: _config.DataVersion,
                data: e.data)
            {
                Id = Guid.NewGuid().ToString(),
                EventTime = DateTimeOffset.UtcNow
            }).ToList();

            await _client.SendEventsAsync(eventGridEvents);

            _logger.LogInformation("Batch of {EventCount} events published successfully", events.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish batch of events");
            throw;
        }
    }
}