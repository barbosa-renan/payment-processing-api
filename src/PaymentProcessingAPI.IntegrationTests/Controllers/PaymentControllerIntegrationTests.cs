using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PaymentProcessingAPI.Infrastructure;
using PaymentProcessingAPI.IntegrationTests.Infrastructure;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;
using System.Net;
using System.Net.Http.Json;

namespace PaymentProcessingAPI.IntegrationTests.Controllers;

public class PaymentControllerIntegrationTests : IClassFixture<PaymentProcessingWebApplicationFactory>
{
    private readonly PaymentProcessingWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PaymentControllerIntegrationTests(PaymentProcessingWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ProcessPayment_ValidCreditCardRequest_ReturnsCreated()
    {
        // Arrange
        var request = CreateValidCreditCardRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.TransactionId.Should().Be(request.TransactionId);
        paymentResponse.Status.Should().BeOneOf(PaymentStatus.Approved, PaymentStatus.Pending, PaymentStatus.Processing);
        paymentResponse.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task ProcessPayment_ValidPIXRequest_ReturnsCreated()
    {
        // Arrange
        var request = CreateValidPIXRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.TransactionId.Should().Be(request.TransactionId);
        paymentResponse.Status.Should().BeOneOf(PaymentStatus.Approved, PaymentStatus.Pending, PaymentStatus.Processing);
    }

    [Fact]
    public async Task ProcessPayment_ValidBoletoRequest_ReturnsCreated()
    {
        // Arrange
        var request = CreateValidBoletoRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.TransactionId.Should().Be(request.TransactionId);
        paymentResponse.Status.Should().BeOneOf(PaymentStatus.Approved, PaymentStatus.Pending, PaymentStatus.Processing);
    }

    [Fact]
    public async Task ProcessPayment_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new PaymentRequest
        {
            TransactionId = "", // Invalid - empty transaction ID
            Amount = -100, // Invalid - negative amount
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.CreditCard
            // Missing required fields
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPaymentStatus_ExistingPayment_ReturnsPayment()
    {
        // Arrange - First create a payment
        var createRequest = CreateValidCreditCardRequest();
        var createResponse = await _client.PostAsJsonAsync("/api/payment/process", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act
        var response = await _client.GetAsync($"/api/payment/{createRequest.TransactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.TransactionId.Should().Be(createRequest.TransactionId);
    }

    [Fact]
    public async Task GetPaymentStatus_NonExistentPayment_ReturnsNotFound()
    {
        // Arrange
        var nonExistentTransactionId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/payment/{nonExistentTransactionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelPayment_ExistingPendingPayment_ReturnsCancelled()
    {
        // Arrange - Create a payment first
        var createRequest = CreateValidBoletoRequest(); // Boleto starts as Pending
        var createResponse = await _client.PostAsJsonAsync("/api/payment/process", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Act
        var response = await _client.PostAsync($"/api/payment/{createRequest.TransactionId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task RefundPayment_ExistingApprovedPayment_ReturnsRefunded()
    {
        // Arrange - Create and approve a payment first
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.CreditCard,
            Status = PaymentStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            Customer = new CustomerInfo
            {
                CustomerId = "CUST001",
                Name = "John Doe",
                Email = "john.doe@example.com",
                Document = "12345678901"
            }
        };
        
        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync();

        var refundRequest = new RefundRequest
        {
            Amount = 50.00m,
            Reason = "Customer request"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/payment/{payment.TransactionId}/refund", refundRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task GetPayments_WithFilter_ReturnsPaginatedResults()
    {
        // Arrange - Create multiple payments
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        
        var payments = new[]
        {
            CreateTestPayment(PaymentStatus.Approved),
            CreateTestPayment(PaymentStatus.Declined),
            CreateTestPayment(PaymentStatus.Approved)
        };
        
        dbContext.Payments.AddRange(payments);
        await dbContext.SaveChangesAsync();

        var filter = new PaymentFilter
        {
            Status = PaymentStatus.Approved,
            Page = 1,
            PageSize = 10
        };

        // Act
        var queryString = $"?Status={filter.Status}&Page={filter.Page}&PageSize={filter.PageSize}";
        var response = await _client.GetAsync($"/api/payment{queryString}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<PaymentResponse>>();
        pagedResult.Should().NotBeNull();
        pagedResult!.Items.Should().HaveCount(2); // Only approved payments
        pagedResult.TotalItems.Should().Be(2);
    }

    [Fact]
    public async Task ProcessPayment_DuplicateTransactionId_ReturnsConflict()
    {
        // Arrange
        var request = CreateValidCreditCardRequest();
        
        // First request
        var firstResponse = await _client.PostAsJsonAsync("/api/payment/process", request);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Second request with same transaction ID
        var secondResponse = await _client.PostAsJsonAsync("/api/payment/process", request);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ProcessPayment_HighValueTransaction_ProcessesSuccessfully()
    {
        // Arrange
        var request = CreateValidCreditCardRequest();
        request.Amount = 15000.00m; // High value transaction

        // Act
        var response = await _client.PostAsJsonAsync("/api/payment/process", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        paymentResponse.Should().NotBeNull();
        paymentResponse!.ProcessedAmount.Should().Be(request.Amount);
    }

    private static PaymentRequest CreateValidCreditCardRequest()
    {
        return new PaymentRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.CreditCard,
            Customer = new CustomerInfo
            {
                CustomerId = "CUST001",
                Name = "John Doe",
                Email = "john.doe@example.com",
                Document = "12345678901",
                Address = new AddressInfo
                {
                    Street = "Main St",
                    Number = "123",
                    City = "São Paulo",
                    State = "SP",
                    ZipCode = "01234567",
                    Country = "Brazil",
                    Neighborhood = "Centro"
                }
            },
            Card = new CardInfo
            {
                Number = "4111111111111111",
                HolderName = "John Doe",
                ExpiryMonth = "12",
                ExpiryYear = "2025",
                CVV = "123",
                Brand = "VISA"
            }
        };
    }

    private static PaymentRequest CreateValidPIXRequest()
    {
        return new PaymentRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 75.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.Pix,
            Customer = new CustomerInfo
            {
                CustomerId = "CUST002",
                Name = "Jane Doe",
                Email = "jane.doe@example.com",
                Document = "98765432100",
                Address = new AddressInfo
                {
                    Street = "Second St",
                    Number = "456",
                    City = "Rio de Janeiro",
                    State = "RJ",
                    ZipCode = "20000000",
                    Country = "Brazil",
                    Neighborhood = "Copacabana"
                }
            }
        };
    }

    private static PaymentRequest CreateValidBoletoRequest()
    {
        return new PaymentRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 150.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.Boleto,
            Customer = new CustomerInfo
            {
                CustomerId = "CUST003",
                Name = "Bob Smith",
                Email = "bob.smith@example.com",
                Document = "11122233344",
                Address = new AddressInfo
                {
                    Street = "Third St",
                    Number = "789",
                    City = "Belo Horizonte",
                    State = "MG",
                    ZipCode = "30000000",
                    Country = "Brazil",
                    Neighborhood = "Centro"
                }
            }
        };
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
                Document = "12345678901"
            }
        };
    }
}