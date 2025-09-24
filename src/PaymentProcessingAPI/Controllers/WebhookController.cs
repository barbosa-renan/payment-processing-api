using Microsoft.AspNetCore.Mvc;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEventPublisherService _eventPublisherService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IPaymentRepository paymentRepository,
        IEventPublisherService eventPublisherService,
        ILogger<WebhookController> logger)
    {
        _paymentRepository = paymentRepository;
        _eventPublisherService = eventPublisherService;
        _logger = logger;
    }

    /// <summary>
    /// Receive payment status webhook notifications
    /// </summary>
    /// <param name="webhook">Webhook payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Acknowledgment of webhook receipt</returns>
    [HttpPost("payment-status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PaymentStatusWebhook(
        [FromBody] PaymentWebhook webhook, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid webhook payload received for transaction {TransactionId}", webhook?.TransactionId);
                return BadRequest("Invalid webhook payload");
            }

            _logger.LogInformation("Webhook received for transaction {TransactionId} with status {Status}", 
                webhook!.TransactionId, webhook.Status);

            // Find the payment
            var payment = await _paymentRepository.GetPaymentAsync(webhook.TransactionId, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for webhook transaction {TransactionId}", webhook.TransactionId);
                return NotFound($"Payment with transaction ID {webhook.TransactionId} not found");
            }

            // Update payment status if it's a valid transition
            if (IsValidStatusTransition(payment.Status, webhook.Status.ToString()))
            {
                var originalStatus = payment.Status;
                payment.Status = webhook.Status.ToString();
                payment.Message = $"Status updated via webhook: {webhook.EventType}";
                payment.UpdatedAt = DateTime.UtcNow;

                await _paymentRepository.UpdatePaymentAsync(payment, cancellationToken);

                // Publish status change event
                var paymentEvent = new PaymentEvent
                {
                    EventType = $"Payment.StatusChanged",
                    TransactionId = webhook.TransactionId,
                    Data = new
                    {
                        PreviousStatus = originalStatus,
                        NewStatus = webhook.Status,
                        EventType = webhook.EventType,
                        webhook.EventDate,
                        WebhookData = webhook.Data
                    },
                    Subject = $"payment/{webhook.TransactionId}/status-changed"
                };

                await _eventPublisherService.PublishPaymentEventAsync(paymentEvent, cancellationToken);

                _logger.LogInformation("Payment status updated for transaction {TransactionId}: {OldStatus} -> {NewStatus}", 
                    webhook.TransactionId, originalStatus, webhook.Status);
            }
            else
            {
                _logger.LogWarning("Invalid status transition for transaction {TransactionId}: {CurrentStatus} -> {NewStatus}", 
                    webhook.TransactionId, payment.Status, webhook.Status);
                
                return BadRequest($"Invalid status transition from {payment.Status} to {webhook.Status}");
            }

            // Return success response
            return Ok(new { message = "Webhook processed successfully", transactionId = webhook.TransactionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for transaction {TransactionId}", webhook?.TransactionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Health check endpoint for webhook service
    /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Validate payment status webhook signature (for security)
    /// </summary>
    /// <param name="signature">Webhook signature</param>
    /// <param name="payload">Webhook payload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Signature validation result</returns>
    [HttpPost("validate-signature")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult ValidateSignature(
        [FromHeader(Name = "X-Webhook-Signature")] string? signature,
        [FromBody] object payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(signature))
            {
                return BadRequest("Missing webhook signature");
            }

            // In a real implementation, you would validate the signature against your secret
            // For now, we'll just simulate validation
            var isValid = ValidateWebhookSignature(signature, payload);

            if (!isValid)
            {
                _logger.LogWarning("Invalid webhook signature received");
                return BadRequest("Invalid webhook signature");
            }

            return Ok(new { valid = true, message = "Signature validated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating webhook signature");
            return StatusCode(500, "Internal server error");
        }
    }

    private static bool IsValidStatusTransition(string currentStatus, string newStatus)
    {
        // Define valid status transitions
        var validTransitions = new Dictionary<string, List<string>>
        {
            [PaymentStatus.Pending.ToString()] = new() { PaymentStatus.Processing.ToString(), PaymentStatus.Cancelled.ToString(), PaymentStatus.Expired.ToString() },
            [PaymentStatus.Processing.ToString()] = new() { PaymentStatus.Approved.ToString(), PaymentStatus.Declined.ToString(), PaymentStatus.Failed.ToString() },
            [PaymentStatus.Approved.ToString()] = new() { PaymentStatus.Refunded.ToString() },
            [PaymentStatus.Declined.ToString()] = new(), // Terminal state
            [PaymentStatus.Cancelled.ToString()] = new(), // Terminal state
            [PaymentStatus.Refunded.ToString()] = new(), // Terminal state
            [PaymentStatus.Failed.ToString()] = new(), // Terminal state
            [PaymentStatus.Expired.ToString()] = new() // Terminal state
        };

        return validTransitions.ContainsKey(currentStatus) && 
               validTransitions[currentStatus].Contains(newStatus);
    }

    private static bool ValidateWebhookSignature(string signature, object payload)
    {
        // In a real implementation, you would:
        // 1. Extract the signature from the header
        // 2. Compute HMAC-SHA256 of the payload using your webhook secret
        // 3. Compare the computed signature with the received signature
        // For this demo, we'll just check if signature is not empty
        return !string.IsNullOrEmpty(signature) && signature.Length > 10;
    }
}