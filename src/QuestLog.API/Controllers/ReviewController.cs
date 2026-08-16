using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.Review.Commands;
using QuestLog.Application.Review.DTOs;
using QuestLog.Application.Review.Queries;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api/review")]
[Produces("application/json")]
public class ReviewController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReviewController(IMediator mediator) => _mediator = mediator;

    // GET /api/review/weekly?week=2026-07-20
    // Any date in the week is accepted — the handler normalizes to Monday.
    [HttpGet("weekly")]
    [ProducesResponseType(typeof(WeeklyReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetWeekly([FromQuery] string week, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(week, "yyyy-MM-dd", out var parsed))
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid date format.",
                Detail = "week must be in yyyy-MM-dd format.",
                Status = StatusCodes.Status400BadRequest
            });

        return Ok(await _mediator.Send(new GetWeeklyReviewQuery(parsed), ct));
    }

    // POST /api/review/reflection
    [HttpPost("reflection")]
    [ProducesResponseType(typeof(ReflectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveReflection([FromBody] SaveReflectionRequest request, CancellationToken ct)
    {
        if (!DateOnly.TryParseExact(request.WeekStart, "yyyy-MM-dd", out var parsed))
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid date format.",
                Detail = "weekStart must be in yyyy-MM-dd format.",
                Status = StatusCodes.Status400BadRequest
            });

        return Ok(await _mediator.Send(new SaveReflectionCommand(parsed, request.Notes), ct));
    }
}

public record SaveReflectionRequest(string WeekStart, string Notes);
