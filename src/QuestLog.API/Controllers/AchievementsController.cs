using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.Achievements.DTOs;
using QuestLog.Application.Achievements.Queries;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api/achievements")]
[Produces("application/json")]
public class AchievementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AchievementsController(IMediator mediator) => _mediator = mediator;

    // GET /api/achievements
    [HttpGet]
    [ProducesResponseType(typeof(List<AchievementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAchievementsQuery(), ct));
}
