using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Achievements.Common;
using QuestLog.Application.Achievements.DTOs;
using QuestLog.Application.Common.DTOs;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.Habits.Common;
using QuestLog.Application.Habits.DTOs;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Habits.Commands.ToggleHabitEntry;

// ── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Toggles the completion status of a habit for a given date.
/// If no entry exists for that date, one is created (IsCompleted = true).
/// If an entry already exists, its IsCompleted flag is flipped.
///
/// On completion (newly flipping to true), the handler awards 10 XP for the
/// habit completion and, if the resulting streak is exactly 7/30/100, awards
/// a streak-milestone bonus. It then runs <see cref="AchievementChecker"/> so
/// achievements like "first_flame", "perfect_week", and streak badges unlock
/// without a separate request. The response envelopes the existing <see cref="HabitDto"/>
/// with the XP/level/achievement deltas.
/// </summary>
public record ToggleHabitEntryCommand(
    int HabitId,
    DateOnly Date
) : IRequest<GamifiedResult<HabitDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class ToggleHabitEntryCommandHandler : IRequestHandler<ToggleHabitEntryCommand, GamifiedResult<HabitDto>>
{
    private readonly IHabitRepository _repository;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;

    public ToggleHabitEntryCommandHandler(
        IHabitRepository repository,
        IXpAwardService xpAwardService,
        IAchievementChecker achievementChecker)
    {
        _repository = repository;
        _xpAwardService = xpAwardService;
        _achievementChecker = achievementChecker;
    }

    public async Task<GamifiedResult<HabitDto>> Handle(ToggleHabitEntryCommand request, CancellationToken cancellationToken)
    {
        // Ensure the habit exists
        var habit = await _repository.GetByIdAsync(request.HabitId, cancellationToken)
            ?? throw new KeyNotFoundException($"Habit with ID {request.HabitId} was not found.");

        // Upsert: find existing entry for this date or create a new one
        var entry = await _repository.GetEntryAsync(request.HabitId, request.Date, cancellationToken);

        bool newlyCompleted;

        if (entry is null)
        {
            // No entry yet → create it as completed
            entry = new HabitEntry
            {
                HabitId     = request.HabitId,
                Date        = request.Date,
                IsCompleted = true,
                CreatedAt   = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };
            _repository.AddEntry(entry);
            newlyCompleted = true;
        }
        else
        {
            // Entry exists → flip the flag
            newlyCompleted = !entry.IsCompleted;
            entry.IsCompleted = !entry.IsCompleted;
            entry.CompletedAt = entry.IsCompleted ? DateTime.UtcNow : null;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        // Reload all completed dates to calculate streak
        var allCompletedDates = await _repository.GetAllCompletedDatesAsync(request.HabitId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool isCompletedToday = allCompletedDates.Contains(today);
        int streak = StreakCalculator.Calculate(allCompletedDates, today);

        var dto = new HabitDto
        {
            Id               = habit.Id,
            Name             = habit.Name,
            Emoji            = habit.Emoji,
            IsArchived       = habit.IsArchived,
            SortOrder        = habit.SortOrder,
            CreatedAt        = habit.CreatedAt,
            IsCompletedToday = isCompletedToday,
            CurrentStreak    = streak
        };

        // Only award XP when the toggle transitioned to completed.
        // Un-completing is a no-op for XP (we do not revoke).
        if (!newlyCompleted)
            return GamifiedResult<HabitDto>.Empty(dto);

        var habitRef = request.HabitId.ToString();

        // Base habit completion XP (10), idempotent per habit per UTC day.
        var baseResult = await _xpAwardService.AwardXpAsync(
            habit.UserId, 10, XpSource.HabitCompletion, habitRef, cancellationToken);

        // Streak milestone bonus, once per habit per threshold.
        int streakBonus = streak switch { 7 => 50, 30 => 100, 100 => 200, _ => 0 };
        XpAwardResult? streakResult = null;
        if (streakBonus > 0)
        {
            streakResult = await _xpAwardService.AwardXpAsync(
                habit.UserId, streakBonus, XpSource.StreakMilestone, habitRef, cancellationToken);
        }

        // Check and unlock any newly earned achievements.
        var unlocked = await _achievementChecker.CheckAndUnlockAsync(cancellationToken);

        return BuildGamifiedResult(dto, baseResult, streakResult, unlocked);
    }

    private static GamifiedResult<HabitDto> BuildGamifiedResult(
        HabitDto dto,
        XpAwardResult baseResult,
        XpAwardResult? streakResult,
        List<Achievement> unlocked)
    {
        int totalXp = baseResult.XpAwarded + (streakResult?.XpAwarded ?? 0);
        bool idempotent = baseResult.Idempotent && (streakResult is null || streakResult.Idempotent);
        int? newLevel = streakResult?.NewLevel ?? baseResult.NewLevel;
        bool levelUp = (streakResult?.LevelUp ?? false) || baseResult.LevelUp;

        var achievementDtos = unlocked
            .Select(a => AchievementMapper.ToDto(a, DateTime.UtcNow))
            .ToList();

        return new GamifiedResult<HabitDto>(
            Data: dto,
            XpAwarded: totalXp,
            NewLevel: newLevel,
            LevelUp: levelUp,
            NewAchievements: achievementDtos,
            Idempotent: idempotent);
    }
}