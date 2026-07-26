using Microsoft.AspNetCore.Http;
using QuestLog.API.Middleware;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Api;

/// <summary>
/// Collects emitted log events so tests can assert enrichment (e.g. CorrelationId).
/// </summary>
internal sealed class CollectingSink : ILogEventSink
{
    public List<LogEvent> Events { get; } = new();
    public void Emit(LogEvent logEvent) => Events.Add(logEvent);
}

public class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Generates_correlation_id_when_header_missing()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var corrId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString();
        corrId.ShouldNotBeNullOrEmpty();
        Guid.TryParse(corrId, out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Echoes_correlation_id_in_response_header()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        var corrId = context.Items[CorrelationIdMiddleware.ItemKey]!.ToString();
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().ShouldBe(corrId);
    }

    [Fact]
    public async Task Uses_provided_correlation_id_header()
    {
        var context = new DefaultHttpContext();
        const string provided = "session-id-from-frontend";
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = provided;
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        context.Items[CorrelationIdMiddleware.ItemKey]!.ToString().ShouldBe(provided);
        context.Response.Headers[CorrelationIdMiddleware.HeaderName].ToString().ShouldBe(provided);
    }

    [Fact]
    public async Task Calls_next_middleware()
    {
        var context = new DefaultHttpContext();
        var called = false;
        var middleware = new CorrelationIdMiddleware(_ =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task Enriches_log_events_within_request_with_correlation_id()
    {
        var sink = new CollectingSink();
        var previousLogger = Log.Logger;
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "test-corr-id";
            var middleware = new CorrelationIdMiddleware(_ =>
            {
                Log.Information("Inside request pipeline");
                return Task.CompletedTask;
            });

            await middleware.InvokeAsync(context);

            var evt = sink.Events.ShouldHaveSingleItem();
            evt.Properties.TryGetValue("CorrelationId", out var value).ShouldBeTrue();
            value!.ToString().ShouldBe("\"test-corr-id\"");
        }
        finally
        {
            Log.Logger = previousLogger;
        }
    }

    [Fact]
    public async Task Log_events_after_request_do_not_carry_correlation_id()
    {
        var sink = new CollectingSink();
        var previousLogger = Log.Logger;
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();

        try
        {
            var context = new DefaultHttpContext();
            var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

            await middleware.InvokeAsync(context);
            Log.Information("After request pipeline");

            var evt = sink.Events.ShouldHaveSingleItem();
            evt.Properties.ContainsKey("CorrelationId").ShouldBeFalse();
        }
        finally
        {
            Log.Logger = previousLogger;
        }
    }
}
