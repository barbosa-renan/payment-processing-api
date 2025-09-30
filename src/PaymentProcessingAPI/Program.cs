using AspNetCoreRateLimit;
using PaymentProcessingAPI.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuration Extensions
builder.AddKeyVaultConfiguration();
await builder.LoadEventGridSecretsAsync();
builder.AddSerilogConfiguration();

// Service Configuration
builder.Services.AddControllerConfiguration();
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddAutoMapperConfiguration();
builder.Services.AddFluentValidationConfiguration();
builder.Services.AddConfigurationOptions(builder.Configuration);

// Azure Services
builder.Services.AddServiceBusConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddEventGridConfiguration();

// Application Services
builder.Services.AddApplicationServices();
builder.Services.AddHttpClientConfiguration();

// Authentication & Security
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddRateLimitingConfiguration(builder.Configuration);

// Health Checks & Documentation
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddSwaggerConfiguration();

var app = builder.Build();

// Middleware Pipeline
app.UseCustomMiddlewares();
app.UseSwaggerInDevelopment();

app.UseHttpsRedirection();
app.UseCors();
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapHealthChecksEndpoints();
app.MapControllers();

// Database Initialization
app.InitializeDatabase();

Log.Information("Payment Processing API starting up");
app.Run();

public partial class Program { }
