using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PaymentProcessingAPI.Infrastructure;
using PaymentProcessingAPI.IntegrationTests.Infrastructure;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Models.Entities;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.IntegrationTests;

public class DatabaseIntegrationTests : IClassFixture<PaymentProcessingWebApplicationFactory>
{
    private readonly PaymentProcessingWebApplicationFactory _factory;

    public DatabaseIntegrationTests(PaymentProcessingWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PaymentRepository_CreateAndRetrievePayment_WorksCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        
        var payment = new Payment
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 100.00m,
            Currency = "BRL",
            PaymentMethod = "CreditCard",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CustomerId = "CUST001",
            CustomerName = "Integration Test User",
            CustomerEmail = "integration@test.com",
            CustomerDocument = "12345678901",
            AddressStreet = "Integration St",
            AddressNumber = "123",
            AddressNeighborhood = "Test",
            AddressCity = "Test City",
            AddressState = "SP",
            AddressZipCode = "12345678",
            AddressCountry = "Brazil",
            ProcessedAmount = 100.00m,
            NetAmount = 95.00m,
            ProcessingFee = 3.00m,
            GatewayFee = 2.00m,
            TotalFees = 5.00m
        };

        // Act
        var createdPayment = await repository.CreatePaymentAsync(payment);
        var retrievedPayment = await repository.GetPaymentAsync(payment.TransactionId);

        // Assert
        createdPayment.Should().NotBeNull();
        createdPayment.TransactionId.Should().Be(payment.TransactionId);
        
        retrievedPayment.Should().NotBeNull();
        retrievedPayment!.TransactionId.Should().Be(payment.TransactionId);
        retrievedPayment.Amount.Should().Be(payment.Amount);
        retrievedPayment.Status.Should().Be(payment.Status);
    }

    [Fact]
    public async Task PaymentRepository_UpdatePayment_WorksCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        
        var payment = new Payment
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 150.00m,
            Currency = "BRL",
            PaymentMethod = "Pix",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            CustomerId = "CUST002",
            CustomerName = "Update Test User",
            CustomerEmail = "update@test.com",
            CustomerDocument = "98765432100",
            AddressStreet = "Update St",
            AddressNumber = "456",
            AddressNeighborhood = "Update",
            AddressCity = "Update City",
            AddressState = "RJ",
            AddressZipCode = "87654321",
            AddressCountry = "Brazil",
            ProcessedAmount = 150.00m,
            NetAmount = 145.00m,
            ProcessingFee = 3.00m,
            GatewayFee = 2.00m,
            TotalFees = 5.00m
        };

        await repository.CreatePaymentAsync(payment);

        // Act
        payment.Status = "Approved";
        payment.ProcessedAt = DateTime.UtcNow;
        payment.AuthorizationCode = "AUTH123456";
        
        var updatedPayment = await repository.UpdatePaymentAsync(payment);
        var retrievedPayment = await repository.GetPaymentAsync(payment.TransactionId);

        // Assert
        updatedPayment.Should().NotBeNull();
        updatedPayment.Status.Should().Be("Approved");
        updatedPayment.ProcessedAt.Should().NotBeNull();
        
        retrievedPayment.Should().NotBeNull();
        retrievedPayment!.Status.Should().Be("Approved");
        retrievedPayment.AuthorizationCode.Should().Be("AUTH123456");
    }

    [Fact]
    public async Task PaymentRepository_GetPaymentsByFilter_WorksCorrectly()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        
        var payments = new[]
        {
            CreateTestPayment("Approved", "CreditCard", 100.00m),
            CreateTestPayment("Declined", "Pix", 50.00m),
            CreateTestPayment("Approved", "CreditCard", 200.00m),
            CreateTestPayment("Pending", "Boleto", 75.00m)
        };

        foreach (var payment in payments)
        {
            await repository.CreatePaymentAsync(payment);
        }

        var filter = new PaymentFilter
        {
            Status = PaymentStatus.Approved,
            PaymentMethod = PaymentMethod.CreditCard,
            Page = 1,
            PageSize = 10
        };

        // Act
        var filteredPayments = await repository.GetPaymentsByFilterAsync(filter);
        var count = await repository.GetPaymentsCountByFilterAsync(filter);

        // Assert
        filteredPayments.Should().HaveCount(2);
        count.Should().Be(2);
        filteredPayments.Should().AllSatisfy(p =>
        {
            p.Status.Should().Be("Approved");
            p.PaymentMethod.Should().Be("CreditCard");
        });
    }

    [Fact]
    public async Task DatabaseContext_ConcurrentAccess_HandlesCorrectly()
    {
        // Arrange
        using var scope1 = _factory.Services.CreateScope();
        using var scope2 = _factory.Services.CreateScope();
        
        var repository1 = scope1.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var repository2 = scope2.ServiceProvider.GetRequiredService<IPaymentRepository>();

        var payment1 = CreateTestPayment("Pending", "CreditCard", 100.00m);
        var payment2 = CreateTestPayment("Pending", "Pix", 150.00m);

        // Act
        var task1 = repository1.CreatePaymentAsync(payment1);
        var task2 = repository2.CreatePaymentAsync(payment2);

        await Task.WhenAll(task1, task2);

        // Assert
        var retrievedPayment1 = await repository1.GetPaymentAsync(payment1.TransactionId);
        var retrievedPayment2 = await repository2.GetPaymentAsync(payment2.TransactionId);

        retrievedPayment1.Should().NotBeNull();
        retrievedPayment2.Should().NotBeNull();
        retrievedPayment1!.TransactionId.Should().Be(payment1.TransactionId);
        retrievedPayment2!.TransactionId.Should().Be(payment2.TransactionId);
    }

    private static Payment CreateTestPayment(string status, string method, decimal amount)
    {
        var customerId = Random.Shared.Next(1000, 9999);
        return new Payment
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = amount,
            Currency = "BRL",
            PaymentMethod = method,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = status == "Approved" ? DateTime.UtcNow : null,
            CustomerId = $"CUST{customerId}",
            CustomerName = "Test User",
            CustomerEmail = "test@example.com",
            CustomerDocument = "12345678901",
            AddressStreet = "Test St",
            AddressNumber = "123",
            AddressNeighborhood = "Test Neighborhood",
            AddressCity = "Test City",
            AddressState = "SP",
            AddressZipCode = "12345678",
            AddressCountry = "Brazil",
            ProcessedAmount = amount,
            NetAmount = amount * 0.95m,
            ProcessingFee = amount * 0.03m,
            GatewayFee = amount * 0.02m,
            TotalFees = amount * 0.05m
        };
    }
}