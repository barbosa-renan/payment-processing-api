using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PaymentProcessingAPI.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate correlation ID if not present
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers.Add("X-Correlation-ID", correlationId);

        var stopwatch = Stopwatch.StartNew();
        
        // Log request
        await LogRequestAsync(context, correlationId);

        // Capture response
        var originalResponseBody = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Log response
            await LogResponseAsync(context, correlationId, stopwatch.ElapsedMilliseconds);
            
            // Copy response back to original stream
            await responseBody.CopyToAsync(originalResponseBody);
            context.Response.Body = originalResponseBody;
        }
    }

    private async Task LogRequestAsync(HttpContext context, string correlationId)
    {
        var request = context.Request;
        
        var logData = new
        {
            CorrelationId = correlationId,
            Method = request.Method,
            Path = request.Path,
            QueryString = request.QueryString.ToString(),
            Headers = GetSanitizedHeaders(request.Headers),
            UserAgent = request.Headers.UserAgent.ToString(),
            RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        };

        // Log request body for POST/PUT requests (excluding sensitive endpoints)
        if ((request.Method == "POST" || request.Method == "PUT") && ShouldLogRequestBody(request.Path))
        {
            request.EnableBuffering();
            var requestBody = await ReadRequestBodyAsync(request);
            
            _logger.LogInformation("HTTP Request: {@LogData} Body: {RequestBody}", 
                logData, SanitizeRequestBody(requestBody));
        }
        else
        {
            _logger.LogInformation("HTTP Request: {@LogData}", logData);
        }
    }

    private async Task LogResponseAsync(HttpContext context, string correlationId, long elapsedMilliseconds)
    {
        var response = context.Response;
        
        var logData = new
        {
            CorrelationId = correlationId,
            StatusCode = response.StatusCode,
            ElapsedMilliseconds = elapsedMilliseconds,
            Headers = GetSanitizedHeaders(response.Headers.ToDictionary(h => h.Key, h => h.Value.AsEnumerable())),
            Timestamp = DateTime.UtcNow
        };

        // Log response body for error responses
        if (response.StatusCode >= 400 && ShouldLogResponseBody(context.Request.Path))
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            
            _logger.LogWarning("HTTP Response: {@LogData} Body: {ResponseBody}", 
                logData, SanitizeResponseBody(responseBody));
        }
        else if (elapsedMilliseconds > 5000) // Log slow requests
        {
            _logger.LogWarning("Slow HTTP Response: {@LogData}", logData);
        }
        else
        {
            _logger.LogInformation("HTTP Response: {@LogData}", logData);
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        request.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        request.Body.Seek(0, SeekOrigin.Begin);
        return body;
    }

    private static Dictionary<string, string> GetSanitizedHeaders(IHeaderDictionary headers)
    {
        var sanitizedHeaders = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            var headerName = header.Key.ToLowerInvariant();
            
            // Mask sensitive headers
            if (IsSensitiveHeader(headerName))
            {
                sanitizedHeaders[header.Key] = "***";
            }
            else
            {
                sanitizedHeaders[header.Key] = string.Join(", ", header.Value!);
            }
        }
        
        return sanitizedHeaders;
    }

    private static Dictionary<string, string> GetSanitizedHeaders(Dictionary<string, IEnumerable<string>> headers)
    {
        var sanitizedHeaders = new Dictionary<string, string>();
        
        foreach (var header in headers)
        {
            var headerName = header.Key.ToLowerInvariant();
            
            if (IsSensitiveHeader(headerName))
            {
                sanitizedHeaders[header.Key] = "***";
            }
            else
            {
                sanitizedHeaders[header.Key] = string.Join(", ", header.Value);
            }
        }
        
        return sanitizedHeaders;
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        var sensitiveHeaders = new[]
        {
            "authorization",
            "x-api-key",
            "x-auth-token",
            "cookie",
            "set-cookie",
            "x-webhook-signature"
        };
        
        return sensitiveHeaders.Contains(headerName);
    }

    private static bool ShouldLogRequestBody(PathString path)
    {
        // Don't log request body for certain paths to avoid logging sensitive data
        var excludedPaths = new[]
        {
            "/api/payment/process", // Contains card information
            "/auth",
            "/login"
        };
        
        return !excludedPaths.Any(excluded => path.StartsWithSegments(excluded));
    }

    private static bool ShouldLogResponseBody(PathString path)
    {
        // Only log response body for error responses on non-sensitive endpoints
        var excludedPaths = new[]
        {
            "/api/payment/process"
        };
        
        return !excludedPaths.Any(excluded => path.StartsWithSegments(excluded));
    }

    private static string SanitizeRequestBody(string requestBody)
    {
        if (string.IsNullOrEmpty(requestBody))
            return requestBody;

        try
        {
            // Try to parse as JSON and sanitize
            var jsonDoc = JsonDocument.Parse(requestBody);
            return SanitizeJsonElement(jsonDoc.RootElement).ToString();
        }
        catch
        {
            // If not valid JSON, apply simple text sanitization
            return SanitizeText(requestBody);
        }
    }

    private static string SanitizeResponseBody(string responseBody)
    {
        if (string.IsNullOrEmpty(responseBody))
            return responseBody;

        try
        {
            // Try to parse as JSON and sanitize
            var jsonDoc = JsonDocument.Parse(responseBody);
            return SanitizeJsonElement(jsonDoc.RootElement).ToString();
        }
        catch
        {
            // If not valid JSON, return as-is for error responses
            return responseBody;
        }
    }

    private static JsonElement SanitizeJsonElement(JsonElement element)
    {
        // This is a simplified implementation
        // In a real scenario, you'd need to properly reconstruct the JSON
        return element;
    }

    private static string SanitizeText(string text)
    {
        // Mask credit card numbers
        text = Regex.Replace(text, @"\b\d{4}[\s-]?\d{4}[\s-]?\d{4}[\s-]?\d{4}\b", "****-****-****-****");
        
        // Mask CVV
        text = Regex.Replace(text, @"(?i)(cvv|cvc)[\s:""]*\d{3,4}", "$1\":\"***\"");
        
        // Mask passwords
        text = Regex.Replace(text, @"(?i)(password|pwd)[\s:""]*[^"",\s}]+", "$1\":\"***\"");
        
        return text;
    }
}