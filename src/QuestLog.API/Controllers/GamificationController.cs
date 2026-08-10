using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.Gamification.DTOs;
using QuestLog.Application.Gamification.Queries;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api/gamification")]
[Produces("application/json")]
public class GamificationController : ControllerBase
{
    private readonly IMediator _mediator;

    public GamificationController(IMediator mediator) => _mediator = mediator;

    // GET /api/gamification/profile
    [HttpGet("profile")]
    [ProducesResponseType(typeof(GamificationProfileDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
        => Ok(await _mediator.Send(new GetGamificationProfileQuery(), ct));
}
