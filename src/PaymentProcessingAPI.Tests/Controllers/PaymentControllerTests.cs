using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentProcessingAPI.Controllers;
using PaymentProcessingAPI.Models;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Tests.Controllers;

public class PaymentControllerTests
{
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<ILogger<PaymentController>> _mockLogger;
    private readonly PaymentController _controller;

    public PaymentControllerTests()
    {
        _mockPaymentService = new Mock<IPaymentService>();
        _mockLogger = new Mock<ILogger<PaymentController>>();
        _controller = new PaymentController(_mockPaymentService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessPayment_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var response = new PaymentResponse
        {
            TransactionId = request.TransactionId,
            Status = PaymentStatus.Approved,
            ProcessedAt = DateTime.UtcNow,
            Message = "Payment approved successfully",
            ProcessedAmount = request.Amount
        };

        _mockPaymentService.Setup(s => s.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        returnedResponse.Status.Should().Be(PaymentStatus.Approved);
    }

    [Fact]
    public async Task ProcessPayment_RateLimitExceeded_ReturnsTooManyRequestsResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var response = new PaymentResponse
        {
            TransactionId = request.TransactionId,
            Status = PaymentStatus.Failed,
            Message = "Rate limit exceeded. Please try again later."
        };

        _mockPaymentService.Setup(s => s.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Should().NotBeNull();
        var statusCodeResult = result.Result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(429);
    }

    [Fact]
    public async Task ProcessPayment_DuplicateTransaction_ReturnsConflictResult()
    {
        // Arrange
        var request = CreateValidPaymentRequest();
        var response = new PaymentResponse
        {
            TransactionId = request.TransactionId,
            Status = PaymentStatus.Failed,
            Message = "Duplicate transaction ID"
        };

        _mockPaymentService.Setup(s => s.ProcessPaymentAsync(It.IsAny<PaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ProcessPayment(request);

        // Assert
        result.Should().NotBeNull();
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.Value.Should().Be(response);
    }

    [Fact]
    public async Task GetPaymentStatus_ExistingPayment_ReturnsOkResult()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var response = new PaymentResponse
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Approved
        };

        _mockPaymentService.Setup(s => s.GetPaymentStatusAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetPaymentStatus(transactionId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        returnedResponse.TransactionId.Should().Be(transactionId);
    }

    [Fact]
    public async Task GetPaymentStatus_NonExistentPayment_ReturnsNotFoundResult()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var response = new PaymentResponse
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Failed,
            Message = "Payment not found"
        };

        _mockPaymentService.Setup(s => s.GetPaymentStatusAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.GetPaymentStatus(transactionId);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelPayment_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var response = new PaymentResponse
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Cancelled,
            Message = "Payment cancelled by user"
        };

        _mockPaymentService.Setup(s => s.CancelPaymentAsync(transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CancelPayment(transactionId);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        returnedResponse.Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task RefundPayment_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var transactionId = Guid.NewGuid().ToString();
        var refundRequest = new RefundRequest
        {
            Amount = 50.00m,
            Reason = "Customer request"
        };
        var response = new PaymentResponse
        {
            TransactionId = transactionId,
            Status = PaymentStatus.Refunded,
            Message = "Refund processed successfully"
        };

        _mockPaymentService.Setup(s => s.RefundPaymentAsync(transactionId, refundRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.RefundPayment(transactionId, refundRequest);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResponse = okResult.Value.Should().BeOfType<PaymentResponse>().Subject;
        returnedResponse.Status.Should().Be(PaymentStatus.Refunded);
    }

    [Fact]
    public async Task GetPayments_ValidFilter_ReturnsOkResult()
    {
        // Arrange
        var filter = new PaymentFilter
        {
            Page = 1,
            PageSize = 10,
            Status = PaymentStatus.Approved
        };

        var pagedResult = new PagedResult<PaymentResponse>
        {
            Items = new List<PaymentResponse>
            {
                new PaymentResponse { TransactionId = Guid.NewGuid().ToString(), Status = PaymentStatus.Approved }
            },
            TotalItems = 1,
            Page = 1,
            PageSize = 10
        };

        _mockPaymentService.Setup(s => s.GetPaymentsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetPayments(filter);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedResult = okResult.Value.Should().BeOfType<PagedResult<PaymentResponse>>().Subject;
        returnedResult.TotalItems.Should().Be(1);
    }

    private static PaymentRequest CreateValidPaymentRequest()
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
}