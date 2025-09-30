using Microsoft.AspNetCore.Mvc;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using PaymentProcessingAPI.Models.EventGrid;

namespace PaymentProcessingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventGridWebhookController : ControllerBase
{
    private readonly ILogger<EventGridWebhookController> _logger;

    public EventGridWebhookController(ILogger<EventGridWebhookController> logger)
    {
        _logger = logger;
    }

    [HttpPost("payment-events")]
    public async Task<IActionResult> HandlePaymentEvents([FromBody] EventGridEvent[] events)
    {
        foreach (var eventGridEvent in events)
        {
            _logger.LogInformation(
                "Received event - Type: {EventType}, Subject: {Subject}, Id: {Id}",
                eventGridEvent.EventType, eventGridEvent.Subject, eventGridEvent.Id);

            if (eventGridEvent.EventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var validationData = eventGridEvent.Data.ToObjectFromJson<SubscriptionValidationEventData>();
                var responseData = new SubscriptionValidationResponse
                {
                    ValidationResponse = validationData.ValidationCode
                };
                return Ok(responseData);
            }

            // Processar eventos específicos
            switch (eventGridEvent.EventType)
            {
                case "PaymentProcessing.Payment.Processed":
                    await HandlePaymentProcessed(eventGridEvent);
                    break;
                case "PaymentProcessing.Payment.Failed":
                    await HandlePaymentFailed(eventGridEvent);
                    break;
                case "PaymentProcessing.Transaction.HighValue":
                    await HandleHighValueTransaction(eventGridEvent);
                    break;
                default:
                    _logger.LogWarning("Unknown event type: {EventType}", eventGridEvent.EventType);
                    break;
            }
        }

        return Ok();
    }

    private async Task HandlePaymentProcessed(EventGridEvent eventGridEvent)
    {
        var paymentData = eventGridEvent.Data.ToObjectFromJson<PaymentProcessedEventData>();
        _logger.LogInformation(
            "Processing payment processed event - TransactionId: {TransactionId}, Amount: {Amount}",
            paymentData.TransactionId, paymentData.Amount);
        
        // TODO: Implementar lógica de processamento
        await Task.CompletedTask;
    }

    private async Task HandlePaymentFailed(EventGridEvent eventGridEvent)
    {
        var failedData = eventGridEvent.Data.ToObjectFromJson<PaymentFailedEventData>();
        _logger.LogInformation(
            "Processing payment failed event - TransactionId: {TransactionId}, Reason: {Reason}",
            failedData.TransactionId, failedData.FailureReason);
        
        //TODO: Implementar lógica de processamento
        await Task.CompletedTask;
    }

    private async Task HandleHighValueTransaction(EventGridEvent eventGridEvent)
    {
        var highValueData = eventGridEvent.Data.ToObjectFromJson<HighValueTransactionEventData>();
        _logger.LogInformation(
            "Processing high value transaction - TransactionId: {TransactionId}, Amount: {Amount}, RiskScore: {RiskScore}",
            highValueData.TransactionId, highValueData.Amount, highValueData.RiskScore);
        
        //TODO: Implementar lógica de processamento
        await Task.CompletedTask;
    }
}