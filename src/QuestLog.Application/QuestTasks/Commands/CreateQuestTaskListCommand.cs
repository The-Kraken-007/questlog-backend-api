using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.QuestTasks.Commands;

public record CreateQuestTaskListCommand(string Name) : IRequest<int>;

public class CreateQuestTaskListCommandValidator : AbstractValidator<CreateQuestTaskListCommand>
{
    public CreateQuestTaskListCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(100);
    }
}

public class CreateQuestTaskListCommandHandler : IRequestHandler<CreateQuestTaskListCommand, int>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateQuestTaskListCommandHandler(IAppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<int> Handle(CreateQuestTaskListCommand request, CancellationToken cancellationToken)
    {
        int maxSort = default;
        
        if (await _context.QuestTaskLists.AnyAsync(cancellationToken))
        {
            maxSort = await _context.QuestTaskLists.MaxAsync(l => l.SortOrder, cancellationToken);
        }

        var entity = new QuestTaskList
        {
            Name = request.Name,
            UserId = _currentUserService.UserId!.Value,
            SortOrder = maxSort + 1
        };

        _context.QuestTaskLists.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.Id;
    }
}
