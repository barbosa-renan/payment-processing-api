using PaymentProcessingAPI.Infrastructure.Repositories;
using PaymentProcessingAPI.Services;
using PaymentProcessingAPI.Services.Interfaces;

namespace PaymentProcessingAPI.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
        services.AddScoped<IEventPublisherService, EventPublisherService>();
        services.AddScoped<IPaymentValidationService, PaymentValidationService>();
        services.AddSingleton<IServiceBusService, ServiceBusService>();
        services.AddSingleton<IEventGridService, EventGridService>();

        return services;
    }

    public static IServiceCollection AddHttpClientConfiguration(this IServiceCollection services)
    {
        services.AddHttpClient<PaymentGatewayService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}