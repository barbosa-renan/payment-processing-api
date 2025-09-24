using FluentAssertions;
using PaymentProcessingAPI.Services;

namespace PaymentProcessingAPI.Tests.Services;

public class PaymentValidationServiceTests
{
    private readonly PaymentValidationService _validationService;

    public PaymentValidationServiceTests()
    {
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentValidationService>();
        _validationService = new PaymentValidationService(logger);
    }

    [Theory]
    [InlineData("4111111111111111", true)] // Valid Visa
    [InlineData("5555555555554444", true)] // Valid Mastercard
    [InlineData("378282246310005", true)] // Valid Amex
    [InlineData("4111-1111-1111-1111", true)] // Valid with dashes
    [InlineData("4111 1111 1111 1111", true)] // Valid with spaces
    [InlineData("1234567890123456", false)] // Invalid Luhn
    [InlineData("411111111111111", false)] // Too short
    [InlineData("41111111111111111", false)] // Too long
    [InlineData("", false)] // Empty
    [InlineData("abc123", false)] // Non-numeric
    public void ValidateCreditCard_VariousInputs_ReturnsExpectedResults(string cardNumber, bool expected)
    {
        // Act
        var result = _validationService.ValidateCreditCard(cardNumber);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("12345678901", true)] // Valid CPF
    [InlineData("00000000000", false)] // Invalid CPF (all zeros)
    [InlineData("11111111111", false)] // Invalid CPF (all ones)
    [InlineData("123.456.789-01", true)] // Valid CPF with formatting
    [InlineData("11222333000181", true)] // Valid CNPJ
    [InlineData("11.222.333/0001-81", true)] // Valid CNPJ with formatting
    [InlineData("00000000000000", false)] // Invalid CNPJ (all zeros)
    [InlineData("1234567890", false)] // Invalid length
    [InlineData("", false)] // Empty
    [InlineData("abc123", false)] // Non-numeric
    public void ValidateDocument_VariousInputs_ReturnsExpectedResults(string document, bool expected)
    {
        // Act
        var result = _validationService.ValidateDocument(document);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000", true)] // Valid GUID
    [InlineData("12345678-1234-1234-1234-123456789012", true)] // Valid GUID format
    [InlineData("not-a-guid", false)] // Invalid format
    [InlineData("", false)] // Empty
    public void ValidateTransactionDuplication_VariousInputs_ReturnsExpectedResults(string transactionId, bool expected)
    {
        // Act
        var result = _validationService.ValidateTransactionDuplication(transactionId);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ValidateRateLimitAsync_MultipleCallsWithinLimit_ReturnsTrue()
    {
        // Arrange
        const string customerId = "CUST001";

        // Act & Assert
        for (int i = 0; i < 5; i++)
        {
            var result = await _validationService.ValidateRateLimitAsync(customerId);
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task ValidateRateLimitAsync_ExceedsLimit_ReturnsFalse()
    {
        // Arrange
        const string customerId = "CUST002";

        // Act - Make requests up to the limit
        for (int i = 0; i < 10; i++)
        {
            await _validationService.ValidateRateLimitAsync(customerId);
        }

        // The next request should be rate limited
        var result = await _validationService.ValidateRateLimitAsync(customerId);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("CUST001", true)]
    public async Task ValidateRateLimitAsync_InvalidCustomerId_ReturnsExpectedResult(string? customerId, bool expected)
    {
        // Act
        var result = await _validationService.ValidateRateLimitAsync(customerId!);

        // Assert
        result.Should().Be(expected);
    }
}