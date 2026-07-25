using Serilog.Context;

namespace QuestLog.API.Middleware;

/// <summary>
/// Assigns a correlation id to every request so all logs for a user session can be
/// traced end-to-end. The frontend generates one id per login session and sends it
/// as the X-Correlation-ID header; if absent (e.g. direct API calls), a new Guid
/// is generated. The id is stored in HttpContext.Items and echoed back as a
/// response header.
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var corrId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(corrId))
        {
            corrId = Guid.NewGuid().ToString();
        }

        context.Items[ItemKey] = corrId;
        context.Response.Headers[HeaderName] = corrId;

        // Push onto Serilog's ambient context so every log event emitted while
        // handling this request carries a top-level "CorrelationId" property.
        using (LogContext.PushProperty(ItemKey, corrId))
        {
            await _next(context);
        }
    }
}
