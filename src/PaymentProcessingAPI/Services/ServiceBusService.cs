using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PaymentProcessingAPI.Configuration;
using PaymentProcessingAPI.Configurations;
using PaymentProcessingAPI.Models.ServiceBus;
using PaymentProcessingAPI.Services.Interfaces;
using System.Text;

namespace PaymentProcessingAPI.Services
{
    public class ServiceBusService : IServiceBusService, IDisposable
    {
        private readonly ServiceBusClient? _client;
        private readonly AzureServiceBusOptions _azureConfig;
        private readonly ServiceBusConfiguration _serviceBusConfig;
        private readonly ILogger<ServiceBusService> _logger;
        private readonly Dictionary<string, ServiceBusSender> _senders;

        public ServiceBusService(
            ServiceBusClient? client,
            IOptions<AzureServiceBusOptions> azureConfig,
            IOptions<ServiceBusConfiguration> serviceBusConfig,
            ILogger<ServiceBusService> logger)
        {
            _client = client;
            _azureConfig = azureConfig.Value;
            _serviceBusConfig = serviceBusConfig.Value;
            _logger = logger;
            _senders = new Dictionary<string, ServiceBusSender>();
        }

        public async Task SendPaymentProcessedAsync(PaymentProcessedMessage message)
        {
            _logger.LogInformation("Sending payment processed message for transaction {TransactionId}", message.TransactionId);
            await SendMessageAsync(_serviceBusConfig.PaymentProcessedQueue, message);
        }

        public async Task SendPaymentFailedAsync(PaymentFailedMessage message)
        {
            _logger.LogInformation("Sending payment failed message for transaction {TransactionId}", message.TransactionId);
            await SendMessageAsync(_serviceBusConfig.PaymentFailedQueue, message);
        }

        public async Task SendNotificationAsync(NotificationMessage message)
        {
            _logger.LogInformation("Sending notification message for customer {CustomerId}", message.CustomerId);
            await SendMessageAsync(_serviceBusConfig.NotificationsQueue, message);
        }

        public async Task SendRefundRequestAsync(RefundRequestMessage message)
        {
            _logger.LogInformation("Sending refund request for transaction {TransactionId}", message.OriginalTransactionId);
            await SendMessageAsync(_serviceBusConfig.RefundRequestsQueue, message);
        }

        public async Task SendHighValueApprovalAsync(HighValueApprovalMessage message)
        {
            _logger.LogInformation("Sending high value approval for transaction {TransactionId}", message.TransactionId);
            await SendMessageAsync(_serviceBusConfig.HighValueApprovalQueue, message);
        }

        public async Task SendMessageAsync<T>(string queueName, T message) where T : class
        {
            if (_client == null)
            {
                _logger.LogWarning("Service Bus client not available. Simulating message send to queue {QueueName} in development mode", queueName);
                
                var json = JsonConvert.SerializeObject(message, new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    NullValueHandling = NullValueHandling.Ignore,
                    Formatting = Formatting.Indented
                });
                
                _logger.LogInformation("📨 [SIMULATED] Message to queue '{QueueName}':\n{MessageContent}", queueName, json);
                
                await Task.Delay(100);
                return;
            }

            try
            {
                var sender = GetOrCreateSender(queueName);
                
                var json = JsonConvert.SerializeObject(message, new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat,
                    NullValueHandling = NullValueHandling.Ignore
                });

                var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
                {
                    ContentType = "application/json",
                    CorrelationId = Guid.NewGuid().ToString(),
                    TimeToLive = TimeSpan.FromHours(24) // TTL de 24 horas
                };

                serviceBusMessage.ApplicationProperties["MessageType"] = typeof(T).Name;
                serviceBusMessage.ApplicationProperties["CreatedAt"] = DateTimeOffset.UtcNow;
                serviceBusMessage.ApplicationProperties["Version"] = "1.0";

                if (message.GetType().GetProperty("TransactionId")?.GetValue(message) is string transactionId)
                {
                    serviceBusMessage.PartitionKey = transactionId;
                }

                await sender.SendMessageAsync(serviceBusMessage);
                
                _logger.LogInformation("Message sent successfully to queue {QueueName}", queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to queue {QueueName}", queueName);
                throw;
            }
        }

        public async Task SendBatchMessagesAsync<T>(string queueName, IEnumerable<T> messages) where T : class
        {
            if (_client == null)
            {
                _logger.LogWarning("Service Bus client not available. Simulating batch message send to queue {QueueName} in development mode", queueName);
                
                var messageList = messages.ToList();
                foreach (var message in messageList)
                {
                    var json = JsonConvert.SerializeObject(message, Formatting.None);
                    _logger.LogInformation("📨 [SIMULATED BATCH] Message to queue '{QueueName}': {MessageContent}", queueName, json);
                }
                
                _logger.LogInformation("📦 [SIMULATED] Batch of {Count} messages sent to queue {QueueName}", messageList.Count, queueName);
                await Task.Delay(200);
                return;
            }

            try
            {
                var sender = GetOrCreateSender(queueName);
                var messageBatch = await sender.CreateMessageBatchAsync();
                
                foreach (var message in messages)
                {
                    var json = JsonConvert.SerializeObject(message);
                    var serviceBusMessage = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
                    {
                        ContentType = "application/json",
                        CorrelationId = Guid.NewGuid().ToString()
                    };

                    if (!messageBatch.TryAddMessage(serviceBusMessage))
                    {
                        // Se não conseguir adicionar, enviar o lote atual e criar um novo
                        await sender.SendMessagesAsync(messageBatch);
                        messageBatch = await sender.CreateMessageBatchAsync();
                        
                        if (!messageBatch.TryAddMessage(serviceBusMessage))
                        {
                            throw new InvalidOperationException("Message too large for batch");
                        }
                    }
                }

                if (messageBatch.Count > 0)
                {
                    await sender.SendMessagesAsync(messageBatch);
                }

                _logger.LogInformation("Batch of {MessageCount} messages sent to queue {QueueName}", 
                    messages.Count(), queueName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch messages to queue {QueueName}", queueName);
                throw;
            }
        }

        private ServiceBusSender GetOrCreateSender(string queueName)
        {
            if (_client == null)
            {
                throw new InvalidOperationException("ServiceBus client is not available. Running in development mode.");
            }

            if (!_senders.ContainsKey(queueName))
            {
                _senders[queueName] = _client.CreateSender(queueName);
            }
            return _senders[queueName];
        }

        public void Dispose()
        {
            try
            {
                foreach (var sender in _senders.Values)
                    sender?.DisposeAsync().AsTask().Wait();
                
                _senders.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing ServiceBusService resources");
            }
        }
    }
}