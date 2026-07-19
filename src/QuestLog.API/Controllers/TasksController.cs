using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.QuestTasks.Commands;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask([FromBody] CreateQuestTaskCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    [HttpPatch("{id}/toggle")]
    public async Task<IActionResult> ToggleTask(int id)
    {
        await _mediator.Send(new ToggleQuestTaskCommand(id));
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        await _mediator.Send(new DeleteQuestTaskCommand(id));
        return NoContent();
    }
}
