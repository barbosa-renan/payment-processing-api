using Microsoft.AspNetCore.Mvc;
using PaymentProcessingAPI.Models.ServiceBus;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IServiceBusService _serviceBusService;
        private readonly ILogger<TestController> _logger;

        public TestController(IServiceBusService serviceBusService, ILogger<TestController> logger)
        {
            _serviceBusService = serviceBusService;
            _logger = logger;
        }

        [HttpPost("service-bus/payment-processed")]
        public async Task<IActionResult> TestPaymentProcessed()
        {
            var message = new PaymentProcessedMessage
            {
                TransactionId = Guid.NewGuid().ToString(),
                CustomerId = "TEST_CUSTOMER_001",
                CustomerEmail = "test@example.com",
                Amount = 150.00m,
                Currency = "BRL",
                PaymentMethod = "CREDIT_CARD",
                AuthorizationCode = "AUTH123456",
                ProcessedAt = DateTime.UtcNow,
                OrderId = Guid.NewGuid().ToString(),
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = "PROD001",
                        ProductName = "Produto Teste",
                        Quantity = 1,
                        UnitPrice = 150.00m
                    }
                }
            };

            await _serviceBusService.SendPaymentProcessedAsync(message);
            
            _logger.LogInformation("Test message sent to payment-processed queue");
            return Ok(new { Message = "Payment processed message sent successfully", TransactionId = message.TransactionId });
        }

        [HttpPost("service-bus/payment-failed")]
        public async Task<IActionResult> TestPaymentFailed()
        {
            var message = new PaymentFailedMessage
            {
                TransactionId = Guid.NewGuid().ToString(),
                CustomerId = "TEST_CUSTOMER_002",
                Amount = 250.00m,
                Currency = "BRL",
                PaymentMethod = "CREDIT_CARD",
                FailureReason = "Insufficient funds",
                ErrorCode = "51",
                FailedAt = DateTime.UtcNow,
                IsRetryable = false
            };

            await _serviceBusService.SendPaymentFailedAsync(message);
            
            _logger.LogInformation("Test message sent to payment-failed queue");
            return Ok(new { Message = "Payment failed message sent successfully", TransactionId = message.TransactionId });
        }

        [HttpPost("service-bus/notification")]
        public async Task<IActionResult> TestNotification()
        {
            var message = new NotificationMessage
            {
                CustomerId = "TEST_CUSTOMER_003",
                TransactionId = Guid.NewGuid().ToString(),
                NotificationType = "PAYMENT_CONFIRMATION",
                Channel = "EMAIL",
                Data = new Dictionary<string, object>
                {
                    { "CustomerName", "João Silva" },
                    { "Amount", 100.00m },
                    { "PaymentMethod", "PIX" }
                },
                CreatedAt = DateTime.UtcNow
            };

            await _serviceBusService.SendNotificationAsync(message);
            
            _logger.LogInformation("Test message sent to notifications queue");
            return Ok(new { Message = "Notification message sent successfully", TransactionId = message.TransactionId });
        }

        [HttpPost("service-bus/refund-request")]
        public async Task<IActionResult> TestRefundRequest()
        {
            var message = new RefundRequestMessage
            {
                RefundId = Guid.NewGuid().ToString(),
                OriginalTransactionId = Guid.NewGuid().ToString(),
                CustomerId = "TEST_CUSTOMER_004",
                RefundAmount = 75.50m,
                Reason = "Customer requested cancellation",
                RequestedAt = DateTime.UtcNow
            };

            await _serviceBusService.SendRefundRequestAsync(message);
            
            _logger.LogInformation("Test message sent to refund-requests queue");
            return Ok(new { Message = "Refund request message sent successfully", RefundId = message.RefundId });
        }

        [HttpPost("service-bus/high-value-approval")]
        public async Task<IActionResult> TestHighValueApproval()
        {
            var message = new HighValueApprovalMessage
            {
                TransactionId = Guid.NewGuid().ToString(),
                CustomerId = "TEST_CUSTOMER_005",
                Amount = 8500.00m,
                Currency = "BRL",
                PaymentMethod = "CREDIT_CARD",
                RiskScore = 45,
                CreatedAt = DateTime.UtcNow
            };

            await _serviceBusService.SendHighValueApprovalAsync(message);
            
            _logger.LogInformation("Test message sent to high-value-approval queue");
            return Ok(new { Message = "High value approval message sent successfully", TransactionId = message.TransactionId });
        }

        [HttpPost("service-bus/test-all")]
        public async Task<IActionResult> TestAllQueues()
        {
            var results = new List<object>();

            try
            {
                // Testar todas as filas
                await TestPaymentProcessed();
                results.Add(new { Queue = "payment-processed", Status = "Success" });

                await Task.Delay(1000); // Pequeno delay entre envios

                await TestPaymentFailed();
                results.Add(new { Queue = "payment-failed", Status = "Success" });

                await Task.Delay(1000);

                await TestNotification();
                results.Add(new { Queue = "notifications", Status = "Success" });

                await Task.Delay(1000);

                await TestRefundRequest();
                results.Add(new { Queue = "refund-requests", Status = "Success" });

                await Task.Delay(1000);

                await TestHighValueApproval();
                results.Add(new { Queue = "high-value-approval", Status = "Success" });

                return Ok(new { Message = "All queues tested successfully", Results = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing queues");
                return StatusCode(500, new { Message = "Error testing queues", Error = ex.Message });
            }
        }
    }
}