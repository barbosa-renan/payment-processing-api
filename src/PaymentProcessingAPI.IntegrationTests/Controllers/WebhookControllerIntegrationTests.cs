using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PaymentProcessingAPI.Infrastructure;
using PaymentProcessingAPI.IntegrationTests.Infrastructure;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;
using System.Net;
using System.Net.Http.Json;

namespace PaymentProcessingAPI.IntegrationTests.Controllers;

public class WebhookControllerIntegrationTests : IClassFixture<PaymentProcessingWebApplicationFactory>
{
    private readonly PaymentProcessingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WebhookControllerIntegrationTests(PaymentProcessingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ReceiveWebhook_ValidPaymentApprovedWebhook_ReturnsOk()
    {
        // Arrange - First create a payment
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payment = CreateTestPayment(PaymentStatus.Pending);
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var webhook = new PaymentWebhook
        {
            TransactionId = payment.TransactionId,
            Status = PaymentStatus.Approved,
            Amount = payment.Amount,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Payment approved"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify payment status was updated
        var updatedPayment = await dbContext.Payments.FindAsync(payment.Id);
        updatedPayment.Should().NotBeNull();
        updatedPayment!.Status.Should().Be(PaymentStatus.Approved);
        updatedPayment.GatewayTransactionId.Should().Be(webhook.GatewayTransactionId);
    }

    [Fact]
    public async Task ReceiveWebhook_ValidPaymentDeclinedWebhook_ReturnsOk()
    {
        // Arrange - First create a payment
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payment = CreateTestPayment(PaymentStatus.Processing);
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var webhook = new PaymentWebhook
        {
            TransactionId = payment.TransactionId,
            Status = PaymentStatus.Declined,
            Amount = payment.Amount,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Insufficient funds"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify payment status was updated
        var updatedPayment = await dbContext.Payments.FindAsync(payment.Id);
        updatedPayment.Should().NotBeNull();
        updatedPayment!.Status.Should().Be(PaymentStatus.Declined);
        updatedPayment.GatewayResponse.Should().Be(webhook.GatewayResponse);
    }

    [Fact]
    public async Task ReceiveWebhook_RefundWebhook_ReturnsOk()
    {
        // Arrange - First create an approved payment
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payment = CreateTestPayment(PaymentStatus.Approved);
        payment.ProcessedAt = DateTime.UtcNow.AddHours(-1);
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var webhook = new PaymentWebhook
        {
            TransactionId = payment.TransactionId,
            Status = PaymentStatus.Refunded,
            Amount = 50.00m, // Partial refund
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = payment.GatewayTransactionId ?? "GTW123456",
            GatewayResponse = "Refund processed"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify payment status was updated
        var updatedPayment = await dbContext.Payments.FindAsync(payment.Id);
        updatedPayment.Should().NotBeNull();
        updatedPayment!.Status.Should().Be(PaymentStatus.Refunded);
        updatedPayment.RefundedAmount.Should().Be(webhook.Amount);
    }

    [Fact]
    public async Task ReceiveWebhook_NonExistentPayment_ReturnsBadRequest()
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = Guid.NewGuid().ToString(), // Non-existent transaction
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Payment approved"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReceiveWebhook_NullWebhook_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/webhook/payment", (PaymentWebhook?)null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task ReceiveWebhook_InvalidTransactionId_ReturnsBadRequest(string transactionId)
    {
        // Arrange
        var webhook = new PaymentWebhook
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Approved,
            Amount = 100.00m,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Payment approved"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReceiveWebhook_DuplicateWebhook_HandlesIdempotently()
    {
        // Arrange - First create a payment
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payment = CreateTestPayment(PaymentStatus.Pending);
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var webhook = new PaymentWebhook
        {
            TransactionId = payment.TransactionId,
            Status = PaymentStatus.Approved,
            Amount = payment.Amount,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Payment approved"
        };

        // Act - Send the same webhook twice
        var firstResponse = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);
        var secondResponse = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify payment status is still correct
        var updatedPayment = await dbContext.Payments.FindAsync(payment.Id);
        updatedPayment.Should().NotBeNull();
        updatedPayment!.Status.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task ReceiveWebhook_HighValueTransactionWebhook_ProcessesSuccessfully()
    {
        // Arrange - First create a high-value payment
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payment = CreateTestPayment(PaymentStatus.Pending);
        payment.Amount = 15000.00m; // High value
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var webhook = new PaymentWebhook
        {
            TransactionId = payment.TransactionId,
            Status = PaymentStatus.Approved,
            Amount = payment.Amount,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "High value payment approved"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhook/payment", webhook);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify payment status was updated
        var updatedPayment = await dbContext.Payments.FindAsync(payment.Id);
        updatedPayment.Should().NotBeNull();
        updatedPayment!.Status.Should().Be(PaymentStatus.Approved);
        updatedPayment.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ReceiveWebhook_PaymentStatusTransition_UpdatesCorrectly()
    {
        // Arrange - Create payment and test status transitions
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payment = CreateTestPayment(PaymentStatus.Pending);
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        // First webhook: Pending -> Processing
        var processingWebhook = new PaymentWebhook
        {
            TransactionId = payment.TransactionId,
            Status = PaymentStatus.Processing,
            Amount = payment.Amount,
            ProcessedAt = DateTime.UtcNow,
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Payment processing"
        };

        // Second webhook: Processing -> Approved
        var approvedWebhook = new PaymentWebhook
        {
            TransactionId = payment.TransactionId,
            Status = PaymentStatus.Approved,
            Amount = payment.Amount,
            ProcessedAt = DateTime.UtcNow.AddMinutes(1),
            GatewayTransactionId = "GTW123456",
            GatewayResponse = "Payment approved"
        };

        // Act
        var processingResponse = await _client.PostAsJsonAsync("/api/webhook/payment", processingWebhook);
        var approvedResponse = await _client.PostAsJsonAsync("/api/webhook/payment", approvedWebhook);

        // Assert
        processingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        approvedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify final status
        var finalPayment = await dbContext.Payments.FindAsync(payment.Id);
        finalPayment.Should().NotBeNull();
        finalPayment!.Status.Should().Be(PaymentStatus.Approved);
    }

    private static Payment CreateTestPayment(PaymentStatus status)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.CreditCard,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = status == PaymentStatus.Approved ? DateTime.UtcNow : null,
            Customer = new CustomerInfo
            {
                CustomerId = "CUST001",
                Name = "Test User",
                Email = "test@example.com",
                Document = "12345678901",
                Address = new AddressInfo
                {
                    Street = "Test St",
                    Number = "123",
                    City = "Test City",
                    State = "TS",
                    ZipCode = "12345678",
                    Country = "Brazil",
                    Neighborhood = "Test"
                }
            },
            Card = new CardInfo
            {
                Number = "4111111111111111",
                HolderName = "Test User",
                ExpiryMonth = "12",
                ExpiryYear = "2025",
                CVV = "123",
                Brand = "VISA"
            },
            GatewayTransactionId = status != PaymentStatus.Pending ? "GTW789012" : null
        };
    }
}