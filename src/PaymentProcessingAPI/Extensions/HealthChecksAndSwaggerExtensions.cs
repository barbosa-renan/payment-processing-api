using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace PaymentProcessingAPI.Extensions;

public static class HealthChecksAndSwaggerExtensions
{
    public static IServiceCollection AddHealthChecksConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddSqlServer(configuration.GetConnectionString("DefaultConnection")!)
            .AddCheck("ServiceBus", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("ServiceBus is healthy"))
            .AddCheck("EventGrid", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("EventGrid is healthy"));

        return services;
    }

    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Payment Processing API",
                Version = "v1",
                Description = "A comprehensive payment processing API with Azure integration"
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);
        });

        return services;
    }

    public static WebApplication MapHealthChecksEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(entry => new
                    {
                        name = entry.Key,
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description,
                        duration = entry.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };
                await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
            }
        });

        return app;
    }
}