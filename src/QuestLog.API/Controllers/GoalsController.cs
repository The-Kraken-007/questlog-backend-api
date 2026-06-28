using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.Goals.Commands.AddMilestone;
using QuestLog.Application.Goals.Commands.CreateGoal;
using QuestLog.Application.Goals.Commands.ToggleMilestone;
using QuestLog.Application.Goals.Commands.UpdateGoal;
using QuestLog.Application.Goals.DTOs;
using QuestLog.Application.Goals.Queries.GetAllGoals;
using QuestLog.Application.Goals.Queries.GetGoalById;
using QuestLog.Domain.Enums;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class GoalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public GoalsController(IMediator mediator) => _mediator = mediator;

    // GET /api/goals
    [HttpGet("goals")]
    [ProducesResponseType(typeof(List<GoalDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAllGoalsQuery(), ct));

    // GET /api/goals/{id}
    [HttpGet("goals/{id:int}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new GetGoalByIdQuery(id), ct));

    // POST /api/goals
    [HttpPost("goals")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateGoalRequest request, CancellationToken ct)
    {
        var command = new CreateGoalCommand(request.Title, request.Description, request.TargetDate);
        var goal    = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
    }

    // PUT /api/goals/{id}
    [HttpPut("goals/{id:int}")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGoalRequest request, CancellationToken ct)
    {
        var command = new UpdateGoalCommand(id, request.Title, request.Description, request.TargetDate, request.Status);
        return Ok(await _mediator.Send(command, ct));
    }

    // POST /api/goals/{id}/milestones
    [HttpPost("goals/{id:int}/milestones")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMilestone(int id, [FromBody] AddMilestoneRequest request, CancellationToken ct)
    {
        var command = new AddMilestoneCommand(id, request.Title);
        var goal    = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = goal.Id }, goal);
    }

    // PUT /api/milestones/{id}/toggle
    [HttpPut("milestones/{id:int}/toggle")]
    [ProducesResponseType(typeof(GoalDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleMilestone(int id, CancellationToken ct)
        => Ok(await _mediator.Send(new ToggleMilestoneCommand(id), ct));
}

// ── Request body models ───────────────────────────────────────────────────────

public record CreateGoalRequest(string Title, string? Description, DateOnly? TargetDate);

public record UpdateGoalRequest(string? Title, string? Description, DateOnly? TargetDate, GoalStatus? Status);

public record AddMilestoneRequest(string Title);
