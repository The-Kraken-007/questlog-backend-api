using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Application.QuestTasks.Commands;

public record DeleteQuestTaskCommand(int Id) : IRequest;

public class DeleteQuestTaskCommandHandler : IRequestHandler<DeleteQuestTaskCommand>
{
    private readonly IAppDbContext _context;

    public DeleteQuestTaskCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteQuestTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _context.QuestTasks
            .Include(t => t.QuestTaskList)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (task == null)
        {
            throw new InvalidOperationException("Task not found.");
        }

        _context.QuestTasks.Remove(task);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
