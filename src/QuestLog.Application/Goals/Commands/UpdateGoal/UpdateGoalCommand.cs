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

namespace QuestLog.Application.Goals.Commands.UpdateGoal;

/// <summary>
/// Partial update — only non-null fields are applied.
/// Passing Status = null leaves the current status unchanged.
///
/// When the status transitions to <see cref="GoalStatus.Completed"/>, the
/// handler awards 100 XP for the goal completion and runs the achievement
/// checker. The response envelopes <see cref="GoalDto"/> with XP / level /
/// achievement deltas. Idempotent: re-completing an already-completed goal
/// (or specifying any non-completing status change) awards no XP.
/// </summary>
public record UpdateGoalCommand(
    int Id,
    string? Title,
    string? Description,
    DateOnly? TargetDate,
    GoalStatus? Status
) : IRequest<GamifiedResult<GoalDto>>;

public class UpdateGoalCommandHandler : IRequestHandler<UpdateGoalCommand, GamifiedResult<GoalDto>>
{
    private readonly IGoalRepository _repository;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;

    public UpdateGoalCommandHandler(
        IGoalRepository repository,
        IXpAwardService xpAwardService,
        IAchievementChecker achievementChecker)
    {
        _repository = repository;
        _xpAwardService = xpAwardService;
        _achievementChecker = achievementChecker;
    }

    public async Task<GamifiedResult<GoalDto>> Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await _repository.GetByIdWithMilestonesAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goal with ID {request.Id} was not found.");

        if (request.Title is not null)
            goal.Title = request.Title.Trim();

        if (request.Description is not null)
            goal.Description = request.Description.Trim();

        if (request.TargetDate.HasValue)
            goal.TargetDate = request.TargetDate;

        // Detect the Active → Completed transition so we only award XP once.
        bool newlyCompleted = false;
        if (request.Status.HasValue)
        {
            newlyCompleted = request.Status.Value == GoalStatus.Completed
                && goal.Status != GoalStatus.Completed;

            goal.Status = request.Status.Value;
            // Stamp CompletedAt when manually marking as completed
            if (request.Status.Value == GoalStatus.Completed && goal.CompletedAt is null)
                goal.CompletedAt = DateTime.UtcNow;
            else if (request.Status.Value != GoalStatus.Completed)
                goal.CompletedAt = null;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        var dto = GoalMapper.ToDto(goal);

        if (!newlyCompleted)
            return GamifiedResult<GoalDto>.Empty(dto);

        var xpResult = await _xpAwardService.AwardXpAsync(
            goal.UserId, 100, XpSource.GoalCompletion, goal.Id.ToString(), cancellationToken);

        var unlocked = await _achievementChecker.CheckAndUnlockAsync(cancellationToken);

        return new GamifiedResult<GoalDto>(
            Data: dto,
            XpAwarded: xpResult.XpAwarded,
            NewLevel: xpResult.NewLevel,
            LevelUp: xpResult.LevelUp,
            NewAchievements: unlocked.Select(a => AchievementMapper.ToDto(a, DateTime.UtcNow)).ToList(),
            Idempotent: xpResult.Idempotent);
    }
}