using PaymentProcessingAPI.Configurations;
using PaymentProcessingAPI.Configuration;

namespace PaymentProcessingAPI.Extensions;

public static class ConfigurationExtensions
{
    public static IServiceCollection AddConfigurationOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureServiceBusOptions>(
            configuration.GetSection(AzureServiceBusOptions.ConfigSectionName));

        services.Configure<ServiceBusConfiguration>(
            configuration.GetSection("ServiceBusConfiguration"));

        services.Configure<AzureEventGridOptions>(
            configuration.GetSection("EventGrid"));

        services.Configure<PaymentGatewayOptions>(
            configuration.GetSection(PaymentGatewayOptions.ConfigSectionName));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.ConfigSectionName));

        services.Configure<EventGridConfiguration>(
            configuration.GetSection("EventGrid"));

        return services;
    }
}