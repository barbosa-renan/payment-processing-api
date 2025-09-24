using PaymentProcessingAPI.Configurations;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace PaymentProcessingAPI.Services;

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly PaymentGatewayOptions _options;
    private readonly ILogger<PaymentGatewayService> _logger;

    public PaymentGatewayService(
        HttpClient httpClient, 
        IOptions<PaymentGatewayOptions> options,
        ILogger<PaymentGatewayService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        
        _httpClient.BaseAddress = new Uri(_options.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_options.ApiKey}");
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<PaymentResponse> ProcessCreditCardAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing credit card payment for transaction {TransactionId}", request.TransactionId);

            // Simulate payment processing
            await Task.Delay(1000, cancellationToken); // Simulate network call

            // Mock response based on card number for testing
            var isApproved = !request.Card?.Number.EndsWith("0000") ?? false;
            
            var response = new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = isApproved ? PaymentStatus.Approved : PaymentStatus.Declined,
                AuthorizationCode = isApproved ? GenerateAuthCode() : null,
                ProcessedAt = DateTime.UtcNow,
                Message = isApproved ? "Payment approved successfully" : "Card declined by issuer",
                ProcessedAmount = request.Amount,
                Fees = CalculateFees(request.Amount, request.PaymentMethod)
            };

            _logger.LogInformation("Credit card payment processed: {Status} for transaction {TransactionId}", 
                response.Status, request.TransactionId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing credit card payment for transaction {TransactionId}", request.TransactionId);
            
            return new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                Message = "Payment processing failed",
                ProcessedAmount = 0
            };
        }
    }

    public async Task<PaymentResponse> ProcessPixAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing PIX payment for transaction {TransactionId}", request.TransactionId);

            // Simulate PIX processing
            await Task.Delay(500, cancellationToken);

            var response = new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = PaymentStatus.Approved,
                AuthorizationCode = GeneratePixCode(),
                ProcessedAt = DateTime.UtcNow,
                Message = "PIX payment processed successfully",
                ProcessedAmount = request.Amount,
                Fees = CalculateFees(request.Amount, request.PaymentMethod)
            };

            _logger.LogInformation("PIX payment processed successfully for transaction {TransactionId}", request.TransactionId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PIX payment for transaction {TransactionId}", request.TransactionId);
            
            return new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                Message = "PIX payment processing failed",
                ProcessedAmount = 0
            };
        }
    }

    public async Task<PaymentResponse> ProcessBoletoAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Boleto payment for transaction {TransactionId}", request.TransactionId);

            // Simulate Boleto generation
            await Task.Delay(800, cancellationToken);

            var response = new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = PaymentStatus.Pending, // Boleto starts as pending
                AuthorizationCode = GenerateBoletoCode(),
                ProcessedAt = DateTime.UtcNow,
                Message = "Boleto generated successfully",
                ProcessedAmount = request.Amount,
                Fees = CalculateFees(request.Amount, request.PaymentMethod)
            };

            _logger.LogInformation("Boleto generated successfully for transaction {TransactionId}", request.TransactionId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Boleto for transaction {TransactionId}", request.TransactionId);
            
            return new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                Message = "Boleto generation failed",
                ProcessedAmount = 0
            };
        }
    }

    public async Task<PaymentResponse> ProcessDebitAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing debit card payment for transaction {TransactionId}", request.TransactionId);

            // Simulate debit processing
            await Task.Delay(1200, cancellationToken);

            // Mock response similar to credit card but with different logic
            var isApproved = !request.Card?.Number.EndsWith("1111") ?? false;
            
            var response = new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = isApproved ? PaymentStatus.Approved : PaymentStatus.Declined,
                AuthorizationCode = isApproved ? GenerateAuthCode() : null,
                ProcessedAt = DateTime.UtcNow,
                Message = isApproved ? "Debit payment approved successfully" : "Insufficient funds",
                ProcessedAmount = request.Amount,
                Fees = CalculateFees(request.Amount, request.PaymentMethod)
            };

            _logger.LogInformation("Debit card payment processed: {Status} for transaction {TransactionId}", 
                response.Status, request.TransactionId);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing debit card payment for transaction {TransactionId}", request.TransactionId);
            
            return new PaymentResponse
            {
                TransactionId = request.TransactionId,
                Status = PaymentStatus.Failed,
                ProcessedAt = DateTime.UtcNow,
                Message = "Debit payment processing failed",
                ProcessedAmount = 0
            };
        }
    }

    private static string GenerateAuthCode() => $"AUTH{Random.Shared.Next(100000, 999999)}";
    private static string GeneratePixCode() => $"PIX{Random.Shared.Next(1000000000, int.MaxValue)}";
    private static string GenerateBoletoCode() => $"BOL{Random.Shared.Next(10000000, 99999999)}";

    private static PaymentFees CalculateFees(decimal amount, PaymentMethod paymentMethod)
    {
        var processingFeePercentage = paymentMethod switch
        {
            PaymentMethod.CreditCard => 0.035m, // 3.5%
            PaymentMethod.Debit => 0.02m,      // 2.0%
            PaymentMethod.Pix => 0.005m,       // 0.5%
            PaymentMethod.Boleto => 0.015m,    // 1.5%
            _ => 0.03m
        };

        var gatewayFee = paymentMethod switch
        {
            PaymentMethod.CreditCard => 0.30m,
            PaymentMethod.Debit => 0.20m,
            PaymentMethod.Pix => 0.10m,
            PaymentMethod.Boleto => 0.25m,
            _ => 0.25m
        };

        var processingFee = amount * processingFeePercentage;
        var totalFees = processingFee + gatewayFee;
        var netAmount = amount - totalFees;

        return new PaymentFees
        {
            ProcessingFee = Math.Round(processingFee, 2),
            GatewayFee = gatewayFee,
            TotalFees = Math.Round(totalFees, 2),
            NetAmount = Math.Round(netAmount, 2)
        };
    }
}