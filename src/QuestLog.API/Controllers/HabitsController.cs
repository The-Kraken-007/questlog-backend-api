using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.Habits.Commands.CreateHabit;
using QuestLog.Application.Habits.Commands.ToggleHabitEntry;
using QuestLog.Application.Habits.Commands.UpdateHabit;
using QuestLog.Application.Habits.DTOs;
using QuestLog.Application.Habits.Queries.GetAllHabits;
using QuestLog.Application.Habits.Queries.GetHabitEntries;

namespace QuestLog.API.Controllers;

/// <summary>
/// Manages habits: create, update, toggle completion, and retrieve entries for the heatmap.
/// </summary>
[ApiController]
[Route("api/habits")]
[Produces("application/json")]
public class HabitsController : ControllerBase
{
    private readonly IMediator _mediator;

    public HabitsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── GET /api/habits ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns all active (non-archived) habits with today's completion status and current streak.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<HabitDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var habits = await _mediator.Send(new GetAllHabitsQuery(), cancellationToken);
        return Ok(habits);
    }

    // ── POST /api/habits ──────────────────────────────────────────────────────

    /// <summary>Creates a new habit.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HabitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateHabitRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateHabitCommand(request.Name, request.Emoji ?? "✅");
        var habit   = await _mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetAll), new { id = habit.Id }, habit);
    }

    // ── PUT /api/habits/{id} ──────────────────────────────────────────────────

    /// <summary>
    /// Updates an existing habit. Only fields provided in the request body are updated.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(HabitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateHabitRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateHabitCommand(id, request.Name, request.Emoji, request.SortOrder, request.IsArchived);
        var habit   = await _mediator.Send(command, cancellationToken);
        return Ok(habit);
    }

    // ── POST /api/habits/{id}/toggle/{date} ───────────────────────────────────

    /// <summary>
    /// Toggles the completion status of a habit for the given date (yyyy-MM-dd).
    /// Creates the entry if it doesn't exist; flips it if it does.
    /// </summary>
    [HttpPost("{id:int}/toggle/{date}")]
    [ProducesResponseType(typeof(HabitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Toggle(
        int id,
        string date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsedDate))
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid date format.",
                Detail = "Date must be in yyyy-MM-dd format (e.g. 2025-06-28).",
                Status = StatusCodes.Status400BadRequest
            });

        var command = new ToggleHabitEntryCommand(id, parsedDate);
        var habit   = await _mediator.Send(command, cancellationToken);
        return Ok(habit);
    }

    // ── GET /api/habits/{id}/entries ──────────────────────────────────────────

    /// <summary>
    /// Returns all entries for a specific habit, optionally filtered by date range.
    /// Used by the frontend calendar heatmap.
    /// </summary>
    [HttpGet("{id:int}/entries")]
    [ProducesResponseType(typeof(List<HabitEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEntries(
        int id,
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken)
    {
        DateOnly? fromDate = null;
        DateOnly? toDate   = null;

        if (from is not null && !DateOnly.TryParseExact(from, "yyyy-MM-dd", out var parsedFrom))
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid 'from' date format.",
                Detail = "Date must be in yyyy-MM-dd format (e.g. 2025-01-01).",
                Status = StatusCodes.Status400BadRequest
            });
        else if (from is not null)
            fromDate = DateOnly.ParseExact(from, "yyyy-MM-dd");

        if (to is not null && !DateOnly.TryParseExact(to, "yyyy-MM-dd", out var parsedTo))
            return BadRequest(new ProblemDetails
            {
                Title  = "Invalid 'to' date format.",
                Detail = "Date must be in yyyy-MM-dd format (e.g. 2025-12-31).",
                Status = StatusCodes.Status400BadRequest
            });
        else if (to is not null)
            toDate = DateOnly.ParseExact(to, "yyyy-MM-dd");

        var query   = new GetHabitEntriesQuery(id, fromDate, toDate);
        var entries = await _mediator.Send(query, cancellationToken);
        return Ok(entries);
    }
}

// ── Request body models ───────────────────────────────────────────────────────

/// <summary>Request body for creating a new habit.</summary>
public record CreateHabitRequest(string Name, string? Emoji);

/// <summary>
/// Request body for updating a habit. All fields are optional — only provided
/// fields will be applied (partial update).
/// </summary>
public record UpdateHabitRequest(
    string? Name,
    string? Emoji,
    int? SortOrder,
    bool? IsArchived);
