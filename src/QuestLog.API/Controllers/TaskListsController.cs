using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestLog.Application.QuestTasks.Commands;
using QuestLog.Application.QuestTasks.Queries;

namespace QuestLog.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TaskListsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TaskListsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetLists()
    {
        var result = await _mediator.Send(new GetQuestTaskListsQuery());
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateList([FromBody] CreateQuestTaskListCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetLists), new { id }, new { id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteList(int id)
    {
        await _mediator.Send(new DeleteQuestTaskListCommand(id));
        return NoContent();
    }
}
