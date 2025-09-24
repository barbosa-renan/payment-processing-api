using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PaymentProcessingAPI.Infrastructure;
using PaymentProcessingAPI.Services.Interfaces;
using Moq;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.EventGrid;

namespace PaymentProcessingAPI.IntegrationTests.Infrastructure;

public class PaymentProcessingWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<PaymentDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add In-Memory database for testing
            services.AddDbContext<PaymentDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Replace Azure Service Bus with mock
            services.Remove(services.FirstOrDefault(s => s.ServiceType == typeof(ServiceBusClient))!);
            services.AddSingleton(_ => Mock.Of<ServiceBusClient>());

            // Replace Azure Event Grid with mock
            services.Remove(services.FirstOrDefault(s => s.ServiceType == typeof(EventGridPublisherClient))!);
            services.AddSingleton(_ => Mock.Of<EventGridPublisherClient>());

            // Ensure the database is created
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<PaymentDbContext>();
            
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Testing");
    }

    public HttpClient CreateClientWithServices(out IServiceScope scope)
    {
        var client = CreateClient();
        scope = Services.CreateScope();
        return client;
    }
}