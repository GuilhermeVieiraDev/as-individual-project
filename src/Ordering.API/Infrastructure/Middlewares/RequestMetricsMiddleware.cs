using System.Diagnostics;
using eShop.Ordering.API.Infrastructure.Telemetry;

namespace eShop.Ordering.API.Infrastructure.Middlewares;

public class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestMetricsMiddleware> _logger;

    public RequestMetricsMiddleware(RequestDelegate next, ILogger<RequestMetricsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        
        try
        {
            // Call the next middleware in the pipeline
            await _next(context);
            
            sw.Stop();
            
            // Record 4xx and 5xx status codes as errors
            if (context.Response.StatusCode >= 400)
            {
                _logger.LogWarning("HTTP {StatusCode} error for {Method} {Path}", 
                    context.Response.StatusCode, 
                    context.Request.Method, 
                    context.Request.Path);
                
                OrderingMetrics.RequestErrors.Add(1);
                
                // Record more specific error types
                if (context.Response.StatusCode == 404)
                {
                    _logger.LogWarning("Route not found: {Method} {Path}", 
                        context.Request.Method, 
                        context.Request.Path);
                }
                else if (context.Response.StatusCode >= 500)
                {
                    OrderingMetrics.ProcessingErrors.Add(1);
                }
                else if (context.Response.StatusCode >= 400 && context.Response.StatusCode < 500)
                {
                    OrderingMetrics.ValidationErrors.Add(1);
                }
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", 
                context.Request.Method, 
                context.Request.Path);
            
            OrderingMetrics.RequestErrors.Add(1);
            OrderingMetrics.ProcessingErrors.Add(1);
            
            // Re-throw the exception to be handled by the exception middleware
            throw;
        }
    }
}

// Extension method to add the middleware to the pipeline
public static class RequestMetricsMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestMetrics(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestMetricsMiddleware>();
    }
}
