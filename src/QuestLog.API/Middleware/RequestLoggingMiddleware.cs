using System.Diagnostics;

namespace QuestLog.API.Middleware;

/// <summary>
/// Emits one structured log line per API request: method, path, status code and
/// elapsed time. Registered inside CorrelationIdMiddleware's LogContext scope, so
/// every line automatically carries the request's CorrelationId property.
///
/// Health checks and Swagger are skipped — they are hit constantly by Azure
/// probes and the dev browser, and would drown out real traffic in the logs.
/// </summary>
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
        var stopwatch = Stopwatch.StartNew();
        var statusCode = context.Response.StatusCode;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch
        {
            // The exception handler (registered upstream) will produce the real 500
            // response; we log here so failed requests are never silent.
            statusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            if (!ShouldSkip(context.Request.Path))
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path.Value,
                    statusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
    }

    private static bool ShouldSkip(PathString path)
        => path.StartsWithSegments("/health")
           || path.StartsWithSegments("/swagger")
           || path.StartsWithSegments("/favicon.ico");
}
