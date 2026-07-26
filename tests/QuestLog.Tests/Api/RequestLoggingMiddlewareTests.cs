using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using QuestLog.API.Middleware;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Api;

/// <summary>
/// In-memory ILogger double — captures rendered messages for assertion without
/// mocking framework internals.
/// </summary>
internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = new();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception)));
}

public class RequestLoggingMiddlewareTests
{
    private static DefaultHttpContext CreateContext(string path = "/api/habits")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = path;
        return context;
    }

    [Fact]
    public async Task Logs_method_path_status_and_elapsed_time()
    {
        var logger = new ListLogger<RequestLoggingMiddleware>();
        var context = CreateContext();
        context.Response.StatusCode = 201;
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        var (level, message) = logger.Entries.ShouldHaveSingleItem();
        level.ShouldBe(LogLevel.Information);
        message.ShouldContain("POST");
        message.ShouldContain("/api/habits");
        message.ShouldContain("201");
        message.ShouldContain("ms");
    }

    [Fact]
    public async Task Calls_next_middleware()
    {
        var logger = new ListLogger<RequestLoggingMiddleware>();
        var context = CreateContext();
        var called = false;
        var middleware = new RequestLoggingMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        }, logger);

        await middleware.InvokeAsync(context);

        called.ShouldBeTrue();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/swagger/index.html")]
    public async Task Skips_logging_for_infrastructure_paths(string path)
    {
        var logger = new ListLogger<RequestLoggingMiddleware>();
        var context = CreateContext(path);
        var middleware = new RequestLoggingMiddleware(_ => Task.CompletedTask, logger);

        await middleware.InvokeAsync(context);

        logger.Entries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Logs_500_and_rethrows_when_downstream_throws()
    {
        var logger = new ListLogger<RequestLoggingMiddleware>();
        var context = CreateContext();
        var middleware = new RequestLoggingMiddleware(
            _ => throw new InvalidOperationException("boom"), logger);

        await Should.ThrowAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        var (_, message) = logger.Entries.ShouldHaveSingleItem();
        message.ShouldContain("500");
        message.ShouldContain("/api/habits");
    }
}
