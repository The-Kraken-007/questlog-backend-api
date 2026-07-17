using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace QuestLog.API.Middleware;

/// <summary>
/// Global exception handling middleware. Converts known exception types into
/// structured RFC 7807 ProblemDetails responses so all errors have a consistent shape.
/// 
/// Maps:
/// - <see cref="ValidationException"/>         → 400 Bad Request with field-level errors
/// - <see cref="UnauthorizedAccessException"/> → 401 Unauthorized
/// - <see cref="InvalidOperationException"/>   → 409 Conflict (e.g. duplicate email)
/// - <see cref="KeyNotFoundException"/>         → 404 Not Found
/// - Any other exception                       → 500 Internal Server Error
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            await HandleUnauthorizedExceptionAsync(context, ex);
        }
        catch (InvalidOperationException ex)
        {
            await HandleConflictExceptionAsync(context, ex);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            await HandleUnexpectedExceptionAsync(context, ex);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode  = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/problem+json";

        // Group errors by field name for a clean response shape
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Title    = "One or more validation errors occurred.",
            Status   = StatusCodes.Status400BadRequest,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static async Task HandleUnauthorizedExceptionAsync(HttpContext context, UnauthorizedAccessException ex)
    {
        context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title    = ex.Message,
            Status   = StatusCodes.Status401Unauthorized,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static async Task HandleConflictExceptionAsync(HttpContext context, InvalidOperationException ex)
    {
        context.Response.StatusCode  = StatusCodes.Status409Conflict;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title    = ex.Message,
            Status   = StatusCodes.Status409Conflict,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static async Task HandleNotFoundExceptionAsync(HttpContext context, KeyNotFoundException ex)
    {
        context.Response.StatusCode  = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title    = "Resource not found.",
            Detail   = ex.Message,
            Status   = StatusCodes.Status404NotFound,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }

    private static async Task HandleUnexpectedExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Title    = "An unexpected error occurred.",
            Detail   = "An internal server error has occurred. Please try again later.",
            Status   = StatusCodes.Status500InternalServerError,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
