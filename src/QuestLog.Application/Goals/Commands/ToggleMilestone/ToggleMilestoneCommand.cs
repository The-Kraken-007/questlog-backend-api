using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Common;
using QuestLog.Application.Goals.DTOs;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Goals.Commands.ToggleMilestone;

public record ToggleMilestoneCommand(int MilestoneId) : IRequest<GoalDto>;

public class ToggleMilestoneCommandHandler : IRequestHandler<ToggleMilestoneCommand, GoalDto>
{
    private readonly IGoalRepository _repository;

    public ToggleMilestoneCommandHandler(IGoalRepository repository) => _repository = repository;

    public async Task<GoalDto> Handle(ToggleMilestoneCommand request, CancellationToken cancellationToken)
    {
        var milestone = await _repository.GetMilestoneByIdAsync(request.MilestoneId, cancellationToken)
            ?? throw new KeyNotFoundException($"Milestone with ID {request.MilestoneId} was not found.");

        // Flip the milestone
        milestone.IsCompleted = !milestone.IsCompleted;
        milestone.CompletedAt = milestone.IsCompleted ? DateTime.UtcNow : null;

        await _repository.SaveChangesAsync(cancellationToken);

        // Reload the parent goal with all milestones to recalculate progress and auto-complete
        var goal = await _repository.GetByIdWithMilestonesAsync(milestone.GoalId, cancellationToken);
        if (goal == null) throw new KeyNotFoundException();

        bool allDone = goal.Milestones.Count > 0 && goal.Milestones.All(m => m.IsCompleted);

        if (allDone && goal.Status != GoalStatus.Completed)
        {
            // Auto-complete: every milestone is ticked — mark the goal done
            goal.Status      = GoalStatus.Completed;
            goal.CompletedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync(cancellationToken);
        }
        else if (!allDone && goal.Status == GoalStatus.Completed)
        {
            // A milestone was un-ticked — revert the goal back to Active
            goal.Status      = GoalStatus.Active;
            goal.CompletedAt = null;
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return GoalMapper.ToDto(goal);
    }
}
