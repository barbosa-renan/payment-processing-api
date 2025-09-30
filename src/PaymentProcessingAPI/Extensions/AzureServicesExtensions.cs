using Azure;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;

namespace PaymentProcessingAPI.Extensions;

public static class AzureServicesExtensions
{
    public static IServiceCollection AddServiceBusConfiguration(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddSingleton(provider =>
        {
            var connectionString = configuration["ServiceBus:ConnectionString"];
            if (string.IsNullOrEmpty(connectionString) && environment.IsDevelopment())
            {
                var logger = provider.GetService<ILogger<Program>>();
                logger?.LogWarning("Service Bus connection string not found. Running without Service Bus (dev).");
                return (ServiceBusClient)null!;
            }
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("ServiceBus connection string not found. Configure 'ServiceBus--ConnectionString' no Key Vault.");

            var options = new ServiceBusClientOptions { TransportType = ServiceBusTransportType.AmqpTcp };
            return new ServiceBusClient(connectionString, options);
        });

        return services;
    }

    public static IServiceCollection AddEventGridConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<EventGridPublisherClient>(sp =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var endpoint = cfg["EventGrid:TopicEndpoint"];
            var accessKey = cfg["EventGrid:AccessKey"];
            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(accessKey))
                throw new InvalidOperationException("EventGrid endpoint/key ausentes.");

            return new EventGridPublisherClient(new Uri(endpoint), new AzureKeyCredential(accessKey),
                new EventGridPublisherClientOptions
                {
                    Retry = { Mode = Azure.Core.RetryMode.Exponential, MaxRetries = 5,
                              Delay = TimeSpan.FromMilliseconds(300), MaxDelay = TimeSpan.FromSeconds(5) }
                });
        });

        return services;
    }
}