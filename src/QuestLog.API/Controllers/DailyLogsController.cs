using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.DailyLogs.Commands.CreateOrUpdateLog;
using QuestLog.Application.DailyLogs.DTOs;
using QuestLog.Application.DailyLogs.Queries.GetLogByDate;
using QuestLog.Application.DailyLogs.Queries.GetLogsInRange;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api/logs")]
[Produces("application/json")]
public class DailyLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DailyLogsController(IMediator mediator) => _mediator = mediator;

    // GET /api/logs/{date}
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDate(string date, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsed))
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid date format.",
                Detail = "Date must be in yyyy-MM-dd format.",
                Status = StatusCodes.Status400BadRequest
            });

        var log = await _mediator.Send(new GetLogByDateQuery(parsed), ct);

        // Return 404 if no log exists for this date — caller decides whether to show a blank editor
        return log is null ? NotFound() : Ok(log);
    }

    // POST /api/logs
    [HttpPost]
    [ProducesResponseType(typeof(DailyLogDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdate([FromBody] CreateOrUpdateLogRequest request, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", out var parsed))
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid date format.",
                Detail = "Date must be in yyyy-MM-dd format.",
                Status = StatusCodes.Status400BadRequest
            });

        var log = await _mediator.Send(new CreateOrUpdateLogCommand(parsed, request.Content), ct);
        return Ok(log);
    }

    // GET /api/logs?from={date}&to={date}
    [HttpGet]
    [ProducesResponseType(typeof(List<DailyLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetInRange(
        [FromQuery] string from,
        [FromQuery] string to,
        CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(from, "yyyy-MM-dd", out var fromDate))
            return BadRequest(new ProblemDetails { Title = "Invalid 'from' date format.", Status = 400 });

        if (!DateOnly.TryParseExact(to, "yyyy-MM-dd", out var toDate))
            return BadRequest(new ProblemDetails { Title = "Invalid 'to' date format.", Status = 400 });

        if (fromDate > toDate)
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid date range.",
                Detail = "'from' date must be before or equal to 'to' date.",
                Status = StatusCodes.Status400BadRequest
            });

        var logs = await _mediator.Send(new GetLogsInRangeQuery(fromDate, toDate), ct);
        return Ok(logs);
    }
}

// ── Request body model ────────────────────────────────────────────────────────

public record CreateOrUpdateLogRequest(string Date, string Content);
