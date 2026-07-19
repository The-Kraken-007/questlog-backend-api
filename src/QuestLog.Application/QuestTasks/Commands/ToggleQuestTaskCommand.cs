using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Application.QuestTasks.Commands;

public record ToggleQuestTaskCommand(int Id) : IRequest;

public class ToggleQuestTaskCommandHandler : IRequestHandler<ToggleQuestTaskCommand>
{
    private readonly IAppDbContext _context;

    public ToggleQuestTaskCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ToggleQuestTaskCommand request, CancellationToken cancellationToken)
    {
        // Must verify list ownership via navigation since filter is on list
        var task = await _context.QuestTasks
            .Include(t => t.QuestTaskList)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found.");
        }

        task.IsCompleted = !task.IsCompleted;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
