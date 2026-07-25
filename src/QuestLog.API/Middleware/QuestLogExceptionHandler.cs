using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace QuestLog.API.Middleware;

/// <summary>
/// Converts exceptions into RFC 7807 ProblemDetails responses so all API errors
/// share one shape. Registered via services.AddExceptionHandler + app.UseExceptionHandler.
///
/// Mapping:
/// - <see cref="ValidationException"/>         → 400 with field-level errors
/// - <see cref="UnauthorizedAccessException"/> → 401
/// - <see cref="KeyNotFoundException"/>        → 404
/// - <see cref="InvalidOperationException"/>   → 409 (e.g. duplicate email)
/// - anything else                             → 500 (also logged as Error)
///
/// Every response carries the request's correlation id as a "correlationId"
/// extension and as the X-Correlation-ID response header, so any error can be
/// traced back to the full request log via the same id.
/// </summary>
public class QuestLogExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<QuestLogExceptionHandler> _logger;

    public QuestLogExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<QuestLogExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, problem) = exception switch
        {
            ValidationException validation => (StatusCodes.Status400BadRequest, CreateValidationProblem(validation)),
            UnauthorizedAccessException unauthorized => (StatusCodes.Status401Unauthorized, CreateProblem(unauthorized.Message, 401)),
            KeyNotFoundException notFound => (StatusCodes.Status404NotFound, CreateProblem(notFound.Message, 404, title: "Resource not found.")),
            InvalidOperationException conflict => (StatusCodes.Status409Conflict, CreateProblem(conflict.Message, 409)),
            _ => HandleUnexpected(exception),
        };

        problem.Instance = httpContext.Request.Path;
        EnrichWithCorrelationId(httpContext, problem);

        httpContext.Response.StatusCode = statusCode;

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });

        return true;
    }

    private (int, ProblemDetails) HandleUnexpected(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception occurred.");
        return (StatusCodes.Status500InternalServerError, CreateProblem(
            "An internal server error has occurred. Please try again later.", 500,
            title: "An unexpected error occurred."));
    }

    private static ProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
        };
    }

    private static ProblemDetails CreateProblem(string detail, int status, string? title = null) => new()
    {
        Title = title ?? detail,
        Detail = title is null ? null : detail,
        Status = status,
    };

    private static void EnrichWithCorrelationId(HttpContext httpContext, ProblemDetails problem)
    {
        if (httpContext.Items[CorrelationIdMiddleware.ItemKey] is string corrId)
        {
            problem.Extensions["correlationId"] = corrId;
            // UseExceptionHandler clears response headers before invoking handlers,
            // so the header set by CorrelationIdMiddleware is gone on this path —
            // re-set it here or error responses lose the trace id.
            httpContext.Response.Headers[CorrelationIdMiddleware.HeaderName] = corrId;
        }
    }
}
