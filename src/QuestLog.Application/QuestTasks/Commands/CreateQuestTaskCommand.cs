using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.QuestTasks.Commands;

public record CreateQuestTaskCommand(int QuestTaskListId, string Name, DateTime? DueDate) : IRequest<int>;

public class CreateQuestTaskCommandValidator : AbstractValidator<CreateQuestTaskCommand>
{
    public CreateQuestTaskCommandValidator()
    {
        RuleFor(v => v.QuestTaskListId).GreaterThan(0);
        RuleFor(v => v.Name).NotEmpty().MaximumLength(200);
    }
}

public class CreateQuestTaskCommandHandler : IRequestHandler<CreateQuestTaskCommand, int>
{
    private readonly IAppDbContext _context;

    public CreateQuestTaskCommandHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(CreateQuestTaskCommand request, CancellationToken cancellationToken)
    {
        var listExists = await _context.QuestTaskLists
            .AnyAsync(l => l.Id == request.QuestTaskListId, cancellationToken);

        if (!listExists)
        {
            throw new InvalidOperationException("Task List not found.");
        }

        var entity = new QuestTask
        {
            QuestTaskListId = request.QuestTaskListId,
            Name = request.Name,
            DueDate = request.DueDate,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.QuestTasks.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
