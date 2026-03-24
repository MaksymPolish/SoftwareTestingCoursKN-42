namespace Lab3.Api.Middleware;

using Lab3.Api.Services;

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
        // Resolve correlation ID provider from request scope
        var correlationIdProvider = context.RequestServices.GetService(typeof(ICorrelationIdProvider)) as ICorrelationIdProvider;
        
        // Generate correlation ID for this request
        var correlationId = string.Empty;
        if (correlationIdProvider != null)
        {
            correlationId = correlationIdProvider.GenerateCorrelationId();
        }
        
        var method = context.Request.Method;
        var path = context.Request.Path;

        await _next(context);

        var statusCode = context.Response.StatusCode;
        if (!string.IsNullOrEmpty(correlationId))
        {
            _logger.LogInformation($"[{correlationId}] HTTP {method} {path} - {statusCode}");
        }
        else
        {
            _logger.LogInformation($"HTTP {method} {path} - {statusCode}");
        }
    }
}
