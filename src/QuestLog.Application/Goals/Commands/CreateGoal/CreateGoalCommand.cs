using MediatR;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.DTOs;
using QuestLog.Application.Goals.Common;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Goals.Commands.CreateGoal;

public record CreateGoalCommand(
    string Title,
    string? Description,
    DateOnly? TargetDate
) : IRequest<GoalDto>;

public class CreateGoalCommandHandler : IRequestHandler<CreateGoalCommand, GoalDto>
{
    private readonly IGoalRepository _repository;

    public CreateGoalCommandHandler(IGoalRepository repository) => _repository = repository;

    public async Task<GoalDto> Handle(CreateGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = new Goal
        {
            Title       = request.Title.Trim(),
            Description = request.Description?.Trim(),
            TargetDate  = request.TargetDate,
            CreatedAt   = DateTime.UtcNow
        };

        _repository.Add(goal);
        await _repository.SaveChangesAsync(cancellationToken);

        return GoalMapper.ToDto(goal);
    }
}
