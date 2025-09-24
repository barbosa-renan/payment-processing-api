using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PaymentProcessingAPI.Configurations;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services;

namespace PaymentProcessingAPI.Tests.Services;

public class PaymentGatewayServiceTests
{
    private readonly Mock<IOptions<PaymentGatewayOptions>> _mockOptions;
    private readonly Mock<ILogger<PaymentGatewayService>> _mockLogger;
    private readonly PaymentGatewayService _service;
    private readonly PaymentGatewayOptions _gatewayOptions;

    public PaymentGatewayServiceTests()
    {
        _gatewayOptions = new PaymentGatewayOptions
        {
            BaseUrl = "https://api.gateway.com",
            ApiKey = "test-api-key",
            Environment = "sandbox",
            TimeoutSeconds = 30
        };

        _mockOptions = new Mock<IOptions<PaymentGatewayOptions>>();
        _mockOptions.Setup(o => o.Value).Returns(_gatewayOptions);

        _mockLogger = new Mock<ILogger<PaymentGatewayService>>();

        _service = new PaymentGatewayService(new HttpClient(), _mockOptions.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessCreditCardAsync_ValidCard_ReturnsApprovedResult()
    {
        // Arrange
        var request = CreateCreditCardRequest();

        // Act
        var result = await _service.ProcessCreditCardAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Approved);
        result.TransactionId.Should().Be(request.TransactionId);
        result.AuthorizationCode.Should().NotBeNullOrEmpty();
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        result.ProcessedAmount.Should().Be(request.Amount);
        result.Fees.Should().NotBeNull();
        result.Message.Should().Be("Payment approved successfully");
    }

    [Fact]
    public async Task ProcessCreditCardAsync_CardEndingIn0000_ReturnsDeclinedResult()
    {
        // Arrange
        var request = CreateCreditCardRequest();
        if (request.Card != null)
        {
            request.Card.Number = "4111111111110000"; // Card ending in 0000 should be declined
        }

        // Act
        var result = await _service.ProcessCreditCardAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Declined);
        result.TransactionId.Should().Be(request.TransactionId);
        result.AuthorizationCode.Should().BeNull();
        result.Message.Should().Be("Card declined by issuer");
    }

    [Fact]
    public async Task ProcessPixAsync_ValidRequest_ReturnsApprovedResult()
    {
        // Arrange
        var request = CreatePixRequest();

        // Act
        var result = await _service.ProcessPixAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Approved);
        result.TransactionId.Should().Be(request.TransactionId);
        result.AuthorizationCode.Should().NotBeNullOrEmpty();
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        result.ProcessedAmount.Should().Be(request.Amount);
        result.Fees.Should().NotBeNull();
        result.Message.Should().Be("PIX payment processed successfully");
    }

    [Fact]
    public async Task ProcessBoletoAsync_ValidRequest_ReturnsPendingResult()
    {
        // Arrange
        var request = CreateBoletoRequest();

        // Act
        var result = await _service.ProcessBoletoAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Pending);
        result.TransactionId.Should().Be(request.TransactionId);
        result.AuthorizationCode.Should().NotBeNullOrEmpty();
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        result.ProcessedAmount.Should().Be(request.Amount);
        result.Fees.Should().NotBeNull();
        result.Message.Should().Be("Boleto generated successfully");
    }

    [Fact]
    public async Task ProcessDebitAsync_ValidCard_ReturnsApprovedResult()
    {
        // Arrange
        var request = CreateDebitCardRequest();

        // Act
        var result = await _service.ProcessDebitAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Approved);
        result.TransactionId.Should().Be(request.TransactionId);
        result.AuthorizationCode.Should().NotBeNullOrEmpty();
        result.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(15));
        result.ProcessedAmount.Should().Be(request.Amount);
        result.Fees.Should().NotBeNull();
        result.Message.Should().Be("Debit payment approved successfully");
    }

    [Fact]
    public async Task ProcessDebitAsync_CardEndingIn1111_ReturnsDeclinedResult()
    {
        // Arrange
        var request = CreateDebitCardRequest();
        if (request.Card != null)
        {
            request.Card.Number = "5555555555551111"; // Card ending in 1111 should be declined for debit
        }

        // Act
        var result = await _service.ProcessDebitAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be(PaymentStatus.Declined);
        result.TransactionId.Should().Be(request.TransactionId);
        result.AuthorizationCode.Should().BeNull();
        result.Message.Should().Be("Insufficient funds");
    }

    [Fact]
    public async Task ProcessCreditCardAsync_NullRequest_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.ProcessCreditCardAsync(null!));
    }

    [Fact]
    public async Task ProcessPixAsync_NullRequest_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.ProcessPixAsync(null!));
    }

    [Fact]
    public async Task ProcessBoletoAsync_NullRequest_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.ProcessBoletoAsync(null!));
    }

    [Fact]
    public async Task ProcessDebitAsync_NullRequest_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.ProcessDebitAsync(null!));
    }

    private static PaymentRequest CreateCreditCardRequest()
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

    private static PaymentRequest CreatePixRequest()
    {
        return new PaymentRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 50.00m,
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

    private static PaymentRequest CreateBoletoRequest()
    {
        return new PaymentRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 75.00m,
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

    private static PaymentRequest CreateDebitCardRequest()
    {
        return new PaymentRequest
        {
            TransactionId = Guid.NewGuid().ToString(),
            Amount = 25.00m,
            Currency = Currency.BRL,
            PaymentMethod = PaymentMethod.Debit,
            Customer = new CustomerInfo
            {
                CustomerId = "CUST004",
                Name = "Alice Johnson",
                Email = "alice.johnson@example.com",
                Document = "55566677788",
                Address = new AddressInfo
                {
                    Street = "Fourth St",
                    Number = "101",
                    City = "Salvador",
                    State = "BA",
                    ZipCode = "40000000",
                    Country = "Brazil",
                    Neighborhood = "Pelourinho"
                }
            },
            Card = new CardInfo
            {
                Number = "5555555555554444",
                HolderName = "Alice Johnson",
                ExpiryMonth = "06",
                ExpiryYear = "2026",
                CVV = "456",
                Brand = "MASTERCARD"
            }
        };
    }
}