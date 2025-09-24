using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PaymentProcessingAPI.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var response = new ErrorResponse();

        switch (exception)
        {
            case ArgumentException argEx:
                response.Message = "Invalid argument provided";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Details = _environment.IsDevelopment() ? argEx.Message : null;
                break;
                
            case UnauthorizedAccessException:
                response.Message = "Unauthorized access";
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;
                
            case KeyNotFoundException:
                response.Message = "Resource not found";
                response.StatusCode = (int)HttpStatusCode.NotFound;
                break;
                
            case TimeoutException:
                response.Message = "Request timeout";
                response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                break;
                
            case InvalidOperationException invOpEx:
                response.Message = "Invalid operation";
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Details = _environment.IsDevelopment() ? invOpEx.Message : null;
                break;
                
            default:
                response.Message = "An error occurred while processing your request";
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Details = _environment.IsDevelopment() ? exception.Message : null;
                break;
        }

        // Add correlation ID if available
        if (context.Items.ContainsKey("CorrelationId"))
        {
            response.CorrelationId = context.Items["CorrelationId"]?.ToString();
        }

        context.Response.StatusCode = response.StatusCode;

        // Sanitize sensitive information from logs
        var sanitizedException = SanitizeException(exception);
        _logger.LogError(sanitizedException, "Exception handled by middleware. Status: {StatusCode}, Message: {Message}", 
            response.StatusCode, response.Message);

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static Exception SanitizeException(Exception exception)
    {
        // Create a copy of the exception with sensitive data masked
        var message = exception.Message;
        var stackTrace = exception.StackTrace;

        // Mask potential sensitive information in exception messages
        message = MaskSensitiveData(message);
        
        if (stackTrace != null)
        {
            stackTrace = MaskSensitiveData(stackTrace);
        }

        // Return a new exception with sanitized information
        return new Exception(message)
        {
            Source = exception.Source,
            HelpLink = exception.HelpLink
        };
    }

    private static string MaskSensitiveData(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Mask credit card numbers (sequences of 13-19 digits)
        input = Regex.Replace(input, @"\b\d{13,19}\b", "****-****-****-****", RegexOptions.IgnoreCase);
        
        // Mask CVV (3-4 digits after specific keywords)
        input = Regex.Replace(input, @"(?i)(cvv|cvc|security.?code)[\s:=]*\d{3,4}", "$1 ***", RegexOptions.IgnoreCase);
        
        // Mask email addresses partially
        input = Regex.Replace(input, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", 
            match => $"{match.Value[0]}****@{match.Value.Split('@')[1]}", RegexOptions.IgnoreCase);
        
        // Mask potential connection strings
        input = Regex.Replace(input, @"(?i)(password|pwd|secret|key|token)[\s:=]+[^\s;,]+", "$1=***", RegexOptions.IgnoreCase);

        return input;
    }

    private class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public string? Details { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}