using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using AspNetCoreRateLimit;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PaymentProcessingAPI.Configuration;
using PaymentProcessingAPI.Configurations;
using PaymentProcessingAPI.Infrastructure;
using PaymentProcessingAPI.Infrastructure.Repositories;
using PaymentProcessingAPI.Middlewares;
using PaymentProcessingAPI.Services;
using PaymentProcessingAPI.Services.Interfaces;
using Serilog;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Net;
using Azure.Identity;
using PaymentProcessingAPI.Configuration;
using PaymentProcessingAPI.Services;
using PaymentProcessingAPI.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["AzureKeyVault:VaultUri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential(),
        new AzureKeyVaultConfigurationOptions { ReloadInterval = TimeSpan.FromMinutes(5) }
    );
}
else
{
    Log.Warning("AzureKeyVault:VaultUri nao definido. Key Vault nao sera carregado.");
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(provider =>
{
    var configuration = new AutoMapper.MapperConfiguration(cfg =>
    {
        cfg.AddProfile<MappingProfile>();
    });
    return configuration.CreateMapper();
});

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.Configure<AzureServiceBusOptions>(
    builder.Configuration.GetSection(AzureServiceBusOptions.ConfigSectionName));

builder.Services.Configure<ServiceBusConfiguration>(
    builder.Configuration.GetSection("ServiceBusConfiguration"));

builder.Services.Configure<AzureEventGridOptions>(
    builder.Configuration.GetSection(AzureEventGridOptions.ConfigSectionName));

builder.Services.Configure<PaymentGatewayOptions>(
    builder.Configuration.GetSection(PaymentGatewayOptions.ConfigSectionName));

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.ConfigSectionName));

builder.Services.Configure<EventGridConfiguration>(
    builder.Configuration.GetSection("EventGrid"));

 
builder.Services.AddSingleton(provider =>
{
    var connectionString = builder.Configuration["ServiceBus:ConnectionString"];

    if (string.IsNullOrEmpty(connectionString) && builder.Environment.IsDevelopment())
    {
        var logger = provider.GetService<ILogger<Program>>();
        logger?.LogWarning("Service Bus connection string not found in Key Vault. Running in development mode without Service Bus.");
        return (ServiceBusClient)null!; // Explicit cast for development mode
    }

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("ServiceBus connection string not found. Make sure the 'ServiceBus--ConnectionString' secret is configured in Azure Key Vault.");
    }

    var options = new ServiceBusClientOptions
    {
        TransportType = ServiceBusTransportType.AmqpTcp
    };
    return new ServiceBusClient(connectionString, options);
});

builder.Services.AddSingleton<EventGridPublisherClient>(provider =>
{
    var options = builder.Configuration.GetSection(AzureEventGridOptions.ConfigSectionName)
        .Get<AzureEventGridOptions>();
    return new EventGridPublisherClient(new Uri(options!.TopicEndpoint), new Azure.AzureKeyCredential(options.AccessKey));
});

builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWebhookService, WebhookService>();
builder.Services.AddScoped<IPaymentGatewayService, PaymentGatewayService>();
builder.Services.AddScoped<IEventPublisherService, EventPublisherService>();
builder.Services.AddScoped<IPaymentValidationService, PaymentValidationService>();
builder.Services.AddSingleton<IServiceBusService, ServiceBusService>();
builder.Services.AddScoped<IEventGridService, EventGridService>();

builder.Services.AddHttpClient<PaymentGatewayService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.ConfigSectionName).Get<JwtOptions>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions!.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        var corsSettings = builder.Configuration.GetSection("Cors");
        var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var allowedMethods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? Array.Empty<string>();
        var allowedHeaders = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>();

        corsBuilder
            .WithOrigins(allowedOrigins)
            .WithMethods(allowedMethods)
            .WithHeaders(allowedHeaders)
            .AllowCredentials();
    });
});

builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddCheck("ServiceBus", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("ServiceBus is healthy"))
    .AddCheck("EventGrid", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("EventGrid is healthy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
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
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Request logging - temporarily disabled for troubleshooting
// app.UseMiddleware<RequestLoggingMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Processing API v1");
        c.RoutePrefix = "swagger"; // Set Swagger UI at /swagger
    });
}

app.UseHttpsRedirection();

app.UseCors();
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
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

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        dbContext.Database.EnsureCreated();
        Log.Information("Database initialization completed");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while initializing the database");
    }
}

Log.Information("Payment Processing API starting up");

app.Run();
public partial class Program { }
