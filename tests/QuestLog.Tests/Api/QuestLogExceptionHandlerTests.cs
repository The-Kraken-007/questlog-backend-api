using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuestLog.API.Middleware;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Api;

public class QuestLogExceptionHandlerTests
{
    private static (QuestLogExceptionHandler handler, ListLogger<QuestLogExceptionHandler> logger) CreateHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();
        var logger = new ListLogger<QuestLogExceptionHandler>();
        var handler = new QuestLogExceptionHandler(
            provider.GetRequiredService<IProblemDetailsService>(),
            logger);
        return (handler, logger);
    }

    private static DefaultHttpContext CreateContext(string? corrId = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/habits";
        context.Response.Body = new MemoryStream();
        if (corrId is not null)
        {
            context.Items[CorrelationIdMiddleware.ItemKey] = corrId;
        }
        return context;
    }

    private static async Task<JsonDocument> ReadProblemAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    [Fact]
    public async Task Validation_exception_returns_400_with_errors_grouped_by_field()
    {
        var (handler, _) = CreateHandler();
        var context = CreateContext();
        var failures = new[]
        {
            new ValidationFailure("Name", "Name is required."),
            new ValidationFailure("Name", "Name must be at least 3 characters."),
            new ValidationFailure("Emoji", "Emoji is required."),
        };
        var exception = new ValidationException(failures);

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(400);
        context.Response.ContentType.ShouldBe("application/problem+json");

        using var body = await ReadProblemAsync(context);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        var errors = body.RootElement.GetProperty("errors");
        errors.GetProperty("Name").GetArrayLength().ShouldBe(2);
        errors.GetProperty("Emoji").GetArrayLength().ShouldBe(1);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public async Task Exception_types_map_to_expected_status_codes(int expectedStatus)
    {
        var (handler, _) = CreateHandler();
        var context = CreateContext();
        Exception exception = expectedStatus switch
        {
            401 => new UnauthorizedAccessException("nope"),
            404 => new KeyNotFoundException("missing"),
            409 => new InvalidOperationException("conflict"),
            _ => new Exception("unexpected"),
        };

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(expectedStatus);

        using var body = await ReadProblemAsync(context);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(expectedStatus);
    }

    [Fact]
    public async Task Adds_correlation_id_extension_to_problem_details()
    {
        var (handler, _) = CreateHandler();
        var context = CreateContext(corrId: "corr-abc-123");

        await handler.TryHandleAsync(context, new Exception("boom"), CancellationToken.None);

        using var body = await ReadProblemAsync(context);
        body.RootElement.GetProperty("correlationId").GetString().ShouldBe("corr-abc-123");
    }

    [Fact]
    public async Task Sets_correlation_id_response_header_on_exception_path()
    {
        var (handler, _) = CreateHandler();
        var context = CreateContext(corrId: "corr-abc-123");

        await handler.TryHandleAsync(context, new Exception("boom"), CancellationToken.None);

        // UseExceptionHandler clears response headers before invoking handlers, so the
        // handler must re-set the header itself or error responses lose the trace id.
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().ShouldBe("corr-abc-123");
    }

    [Fact]
    public async Task Unexpected_exception_logs_error()
    {
        var (handler, logger) = CreateHandler();
        var context = CreateContext();

        await handler.TryHandleAsync(context, new Exception("boom"), CancellationToken.None);

        var (level, _) = logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task Known_exceptions_do_not_log_error()
    {
        var (handler, logger) = CreateHandler();
        var context = CreateContext();

        await handler.TryHandleAsync(context, new KeyNotFoundException("missing"), CancellationToken.None);

        logger.Entries.ShouldBeEmpty();
    }
}
