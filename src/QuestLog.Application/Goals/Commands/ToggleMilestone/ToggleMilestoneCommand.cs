using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Achievements.Common;
using QuestLog.Application.Achievements.DTOs;
using QuestLog.Application.Common.DTOs;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.Goals.Common;
using QuestLog.Application.Goals.DTOs;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Goals.Commands.ToggleMilestone;

/// <summary>
/// Flips a milestone's completion flag and auto-completes / reverts the
/// parent goal when all / some milestones are ticked. Awards XP for both the
/// milestone completion (25 XP) and the goal auto-completion (100 XP) when
/// those transitions occur. Un-ticking does not revoke XP.
/// </summary>
public record ToggleMilestoneCommand(int MilestoneId) : IRequest<GamifiedResult<GoalDto>>;

public class ToggleMilestoneCommandHandler : IRequestHandler<ToggleMilestoneCommand, GamifiedResult<GoalDto>>
{
    private readonly IGoalRepository _repository;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;

    public ToggleMilestoneCommandHandler(
        IGoalRepository repository,
        IXpAwardService xpAwardService,
        IAchievementChecker achievementChecker)
    {
        _repository = repository;
        _xpAwardService = xpAwardService;
        _achievementChecker = achievementChecker;
    }

    public async Task<GamifiedResult<GoalDto>> Handle(ToggleMilestoneCommand request, CancellationToken cancellationToken)
    {
        var milestone = await _repository.GetMilestoneByIdAsync(request.MilestoneId, cancellationToken)
            ?? throw new KeyNotFoundException($"Milestone with ID {request.MilestoneId} was not found.");

        bool newlyCompleted = !milestone.IsCompleted;

        // Flip the milestone
        milestone.IsCompleted = !milestone.IsCompleted;
        milestone.CompletedAt = milestone.IsCompleted ? DateTime.UtcNow : null;

        await _repository.SaveChangesAsync(cancellationToken);

        // Reload the parent goal with all milestones to recalculate progress and auto-complete
        var goal = await _repository.GetByIdWithMilestonesAsync(milestone.GoalId, cancellationToken);
        if (goal == null) throw new KeyNotFoundException();

        bool goalAutoCompleted = false;

        bool allDone = goal.Milestones.Count > 0 && goal.Milestones.All(m => m.IsCompleted);

        if (allDone && goal.Status != GoalStatus.Completed)
        {
            // Auto-complete: every milestone is ticked — mark the goal done
            goalAutoCompleted = true;
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

        var dto = GoalMapper.ToDto(goal);

        if (!newlyCompleted)
            return GamifiedResult<GoalDto>.Empty(dto);

        // Milestone completion XP — idempotent per milestone id.
        var milestoneResult = await _xpAwardService.AwardXpAsync(
            goal.UserId, 25, XpSource.GoalMilestone, milestone.Id.ToString(), cancellationToken);

        // Goal auto-completion XP — idempotent per goal id.
        XpAwardResult? goalResult = null;
        if (goalAutoCompleted)
        {
            goalResult = await _xpAwardService.AwardXpAsync(
                goal.UserId, 100, XpSource.GoalCompletion, goal.Id.ToString(), cancellationToken);
        }

        var unlocked = await _achievementChecker.CheckAndUnlockAsync(cancellationToken);

        int totalXp = milestoneResult.XpAwarded + (goalResult?.XpAwarded ?? 0);
        bool idempotent = milestoneResult.Idempotent && (goalResult is null || goalResult.Idempotent);
        int? newLevel = goalResult?.NewLevel ?? milestoneResult.NewLevel;
        bool levelUp = (goalResult?.LevelUp ?? false) || milestoneResult.LevelUp;

        return new GamifiedResult<GoalDto>(
            Data: dto,
            XpAwarded: totalXp,
            NewLevel: newLevel,
            LevelUp: levelUp,
            NewAchievements: unlocked.Select(a => AchievementMapper.ToDto(a, DateTime.UtcNow)).ToList(),
            Idempotent: idempotent);
    }
}