using Microsoft.AspNetCore.Mvc;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PaymentProcessingAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// Process a new payment
    /// </summary>
    /// <param name="request">Payment details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment response with status and details</returns>
    [HttpPost("process")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> ProcessPayment(
        [FromBody] PaymentRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for payment request: {TransactionId}", request?.TransactionId);
                return BadRequest(ModelState);
            }

            var response = await _paymentService.ProcessPaymentAsync(request!, cancellationToken);

            return response.Status switch
            {
                PaymentStatus.Failed when response.Message.Contains("Rate limit") => StatusCode(429, response),
                PaymentStatus.Failed when response.Message.Contains("Duplicate") => Conflict(response),
                PaymentStatus.Failed => BadRequest(response),
                _ => Ok(response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing payment");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get payment status by transaction ID
    /// </summary>
    /// <param name="transactionId">Unique transaction identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment status and details</returns>
    [HttpGet("{transactionId}")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> GetPaymentStatus(
        [Required] string transactionId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _paymentService.GetPaymentStatusAsync(transactionId, cancellationToken);
            
            if (response.Status == PaymentStatus.Failed && response.Message.Contains("not found"))
            {
                return NotFound($"Payment with transaction ID {transactionId} not found");
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment status for transaction {TransactionId}", transactionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Cancel a pending payment
    /// </summary>
    /// <param name="transactionId">Unique transaction identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment status</returns>
    [HttpPost("{transactionId}/cancel")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> CancelPayment(
        [Required] string transactionId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _paymentService.CancelPaymentAsync(transactionId, cancellationToken);
            
            return response.Status switch
            {
                PaymentStatus.Failed when response.Message.Contains("not found") => NotFound($"Payment with transaction ID {transactionId} not found"),
                PaymentStatus.Failed when response.Message.Contains("Cannot cancel") => BadRequest(response.Message),
                _ => Ok(response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment for transaction {TransactionId}", transactionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Refund a processed payment
    /// </summary>
    /// <param name="transactionId">Unique transaction identifier</param>
    /// <param name="request">Refund details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated payment status</returns>
    [HttpPost("{transactionId}/refund")]
    [ProducesResponseType(typeof(PaymentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentResponse>> RefundPayment(
        [Required] string transactionId, 
        [FromBody] RefundRequest request, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _paymentService.RefundPaymentAsync(transactionId, request, cancellationToken);
            
            return response.Status switch
            {
                PaymentStatus.Failed when response.Message.Contains("not found") => NotFound($"Payment with transaction ID {transactionId} not found"),
                PaymentStatus.Failed when (response.Message.Contains("Cannot refund") || response.Message.Contains("exceed")) => BadRequest(response.Message),
                _ => Ok(response)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Get payments with optional filtering
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of payments</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<PaymentResponse>>> GetPayments(
        [FromQuery] PaymentFilter filter, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate pagination parameters
            if (filter.Page < 1)
                filter.Page = 1;
                
            if (filter.PageSize < 1 || filter.PageSize > 100)
                filter.PageSize = 10;

            var result = await _paymentService.GetPaymentsAsync(filter, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments with filter");
            return StatusCode(500, "Internal server error");
        }
    }
}