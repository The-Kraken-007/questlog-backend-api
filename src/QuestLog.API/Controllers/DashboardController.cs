using MediatR;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.Dashboard.DTOs;
using QuestLog.Application.Dashboard.Queries;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    // GET /api/dashboard
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
        => Ok(await _mediator.Send(new GetDashboardDataQuery(), ct));
}
