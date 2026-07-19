using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Application.QuestTasks.Commands;

public record DeleteQuestTaskListCommand(int Id) : IRequest;

public class DeleteQuestTaskListCommandHandler : IRequestHandler<DeleteQuestTaskListCommand>
{
    private readonly IAppDbContext _context;

    public DeleteQuestTaskListCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteQuestTaskListCommand request, CancellationToken cancellationToken)
    {
        var list = await _context.QuestTaskLists
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken);

        if (list == null)
        {
            throw new InvalidOperationException("Task List not found.");
        }

        _context.QuestTaskLists.Remove(list);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
