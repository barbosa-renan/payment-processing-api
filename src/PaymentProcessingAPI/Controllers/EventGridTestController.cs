using Microsoft.AspNetCore.Mvc;
using PaymentProcessingAPI.Models.EventGrid;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventGridTestController : ControllerBase
{
    private readonly IEventGridService _eventGridService;
    private readonly ILogger<EventGridTestController> _logger;

    public EventGridTestController(
        IEventGridService eventGridService, 
        ILogger<EventGridTestController> logger)
    {
        _eventGridService = eventGridService;
        _logger = logger;
    }

    [HttpPost("payment-processed")]
    public async Task<IActionResult> TestPaymentProcessed()
    {
        var eventData = new PaymentProcessedEventData
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 150.00m,
            Currency = "BRL",
            Status = "Approved",
            ProcessedAt = DateTime.UtcNow,
            CustomerId = "CUST001",
            PaymentMethod = "CREDIT_CARD",
            AuthorizationCode = "AUTH" + new Random().Next(100000, 999999)
        };

        await _eventGridService.PublishPaymentProcessedAsync(eventData);

        return Ok(new 
        { 
            Message = "Payment processed event published", 
            TransactionId = eventData.TransactionId 
        });
    }

    [HttpPost("payment-failed")]
    public async Task<IActionResult> TestPaymentFailed()
    {
        var eventData = new PaymentFailedEventData
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 250.00m,
            Currency = "BRL",
            FailureReason = "Insufficient funds",
            ErrorCode = "51",
            CustomerId = "CUST002",
            Retryable = false
        };

        await _eventGridService.PublishPaymentFailedAsync(eventData);

        return Ok(new 
        { 
            Message = "Payment failed event published", 
            TransactionId = eventData.TransactionId 
        });
    }

    [HttpPost("high-value-transaction")]
    public async Task<IActionResult> TestHighValueTransaction()
    {
        var eventData = new HighValueTransactionEventData
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 8500.00m,
            Currency = "BRL",
            CustomerId = "CUST003",
            RiskScore = 45,
            RequiresApproval = true,
            CustomerTier = "Gold"
        };

        await _eventGridService.PublishHighValueTransactionAsync(eventData);

        return Ok(new 
        { 
            Message = "High value transaction event published", 
            TransactionId = eventData.TransactionId 
        });
    }

    [HttpPost("payment-refunded")]
    public async Task<IActionResult> TestPaymentRefunded()
    {
        var eventData = new PaymentRefundedEventData
        {
            TransactionId = Guid.NewGuid().ToString(),
            OriginalTransactionId = Guid.NewGuid().ToString(),
            RefundAmount = 150.00m,
            Currency = "BRL",
            CustomerId = "CUST004",
            RefundReason = "Customer requested cancellation",
            RefundedAt = DateTime.UtcNow
        };

        await _eventGridService.PublishPaymentRefundedAsync(eventData);

        return Ok(new 
        { 
            Message = "Payment refunded event published", 
            TransactionId = eventData.TransactionId 
        });
    }

    [HttpPost("payment-status-changed")]
    public async Task<IActionResult> TestPaymentStatusChanged()
    {
        var eventData = new PaymentStatusChangedEventData
        {
            TransactionId = Guid.NewGuid().ToString(),
            PreviousStatus = "Processing",
            NewStatus = "Approved",
            ChangedAt = DateTime.UtcNow,
            Reason = "Gateway confirmation received"
        };

        await _eventGridService.PublishPaymentStatusChangedAsync(eventData);

        return Ok(new 
        { 
            Message = "Payment status changed event published", 
            TransactionId = eventData.TransactionId 
        });
    }

    [HttpPost("test-all")]
    public async Task<IActionResult> TestAllEvents()
    {
        var results = new List<object>();

        try
        {
            // 1. Payment Processed
            await TestPaymentProcessed();
            results.Add(new { EventType = "PaymentProcessed", Status = "Success" });
            await Task.Delay(500);

            // 2. Payment Failed
            await TestPaymentFailed();
            results.Add(new { EventType = "PaymentFailed", Status = "Success" });
            await Task.Delay(500);

            // 3. High Value Transaction
            await TestHighValueTransaction();
            results.Add(new { EventType = "HighValueTransaction", Status = "Success" });
            await Task.Delay(500);

            // 4. Payment Refunded
            await TestPaymentRefunded();
            results.Add(new { EventType = "PaymentRefunded", Status = "Success" });
            await Task.Delay(500);

            // 5. Payment Status Changed
            await TestPaymentStatusChanged();
            results.Add(new { EventType = "PaymentStatusChanged", Status = "Success" });

            return Ok(new 
            { 
                Message = "All events published successfully", 
                Results = results 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing events");
            return StatusCode(500, new { Message = "Error testing events", Error = ex.Message });
        }
    }

    [HttpPost("batch-test")]
    public async Task<IActionResult> TestBatchPublish([FromQuery] int eventCount = 5)
    {
        try
        {
            var events = new List<(string eventType, string subject, PaymentProcessedEventData data)>();

            for (int i = 0; i < eventCount; i++)
            {
                var transactionId = Guid.NewGuid().ToString();
                var eventData = new PaymentProcessedEventData
                {
                    TransactionId = transactionId,
                    Amount = new Random().Next(50, 500),
                    Currency = "BRL",
                    Status = "Approved",
                    ProcessedAt = DateTime.UtcNow,
                    CustomerId = $"CUST{i:D3}",
                    PaymentMethod = "CREDIT_CARD",
                    AuthorizationCode = $"AUTH{new Random().Next(100000, 999999)}"
                };

                events.Add((
                    "PaymentProcessing.Payment.Processed",
                    $"payment/{transactionId}",
                    eventData
                ));
            }

            await _eventGridService.PublishEventsAsync(events);

            return Ok(new 
            { 
                Message = $"Batch of {eventCount} events published successfully" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing batch events");
            return StatusCode(500, new { Message = "Error publishing batch", Error = ex.Message });
        }
    }
}