using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.Common;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Common.Services;

/// <summary>
/// Evaluates all achievement conditions for a user and unlocks any that are newly met.
/// Called after XP-awarding actions (habit toggle, goal completion, daily log save).
/// </summary>
public class AchievementChecker
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public AchievementChecker(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Check all achievement conditions and unlock any newly earned ones.
    /// Returns the list of newly unlocked achievements (empty if none).
    /// </summary>
    public async Task<List<Achievement>> CheckAndUnlockAsync(CancellationToken ct = default)
    {
        var userId = _currentUserService.UserId;
        if (userId is null) return [];

        var uid = userId.Value;

        // Get already-unlocked achievement keys to avoid re-checking
        var unlockedIds = await _db.UserAchievements
            .Where(ua => ua.UserId == uid)
            .Select(ua => ua.AchievementId)
            .ToHashSetAsync(ct);

        var allAchievements = await _db.Achievements.ToListAsync(ct);
        var newlyUnlocked = new List<Achievement>();

        foreach (var achievement in allAchievements)
        {
            if (unlockedIds.Contains(achievement.Id))
                continue;

            bool earned = await EvaluateConditionAsync(achievement.Key, uid, ct);
            if (earned)
            {
                _db.UserAchievements.Add(new UserAchievement
                {
                    UserId = uid,
                    AchievementId = achievement.Id,
                    UnlockedAt = DateTime.UtcNow
                });
                newlyUnlocked.Add(achievement);
            }
        }

        if (newlyUnlocked.Count > 0)
            await _db.SaveChangesAsync(ct);

        return newlyUnlocked;
    }

    private async Task<bool> EvaluateConditionAsync(string key, Guid userId, CancellationToken ct)
    {
        return key switch
        {
            "first_flame" => await _db.HabitEntries
                .AnyAsync(e => e.Habit.UserId == userId && e.IsCompleted, ct),

            "week_warrior" => await HasStreakOfAtLeast(userId, 7, ct),
            "monthly_master" => await HasStreakOfAtLeast(userId, 30, ct),
            "century_club" => await HasStreakOfAtLeast(userId, 100, ct),

            "goal_getter" => await _db.Goals
                .CountAsync(g => g.UserId == userId && g.Status == GoalStatus.Completed, ct) >= 1,

            "overachiever" => await _db.Goals
                .CountAsync(g => g.UserId == userId && g.Status == GoalStatus.Completed, ct) >= 10,

            "milestone_marker" => await _db.Milestones
                .CountAsync(m => m.Goal.UserId == userId && m.IsCompleted, ct) >= 5,

            "perfect_week" => await HasPerfectWeek(userId, ct),

            "dedicated" => await HasDistinctDaysWithCompletions(userId, GetStartOfWeek(), DateOnly.FromDateTime(DateTime.UtcNow), 5, ct),

            "committed" => await HasDistinctDaysWithCompletions(userId, GetStartOfMonth(), DateOnly.FromDateTime(DateTime.UtcNow), 20, ct),

            "unstoppable" => await _db.HabitEntries
                .CountAsync(e => e.Habit.UserId == userId && e.IsCompleted, ct) >= 100,

            "rising_star" => await HasReachedLevel(userId, 10, ct),
            "level_legend" => await HasReachedLevel(userId, 50, ct),

            "night_owl" => await _db.HabitEntries
                .AnyAsync(e => e.Habit.UserId == userId && e.IsCompleted
                    && e.CompletedAt.HasValue && e.CompletedAt.Value.Hour >= 0 && e.CompletedAt.Value.Hour < 4, ct),

            "early_bird" => await _db.HabitEntries
                .AnyAsync(e => e.Habit.UserId == userId && e.IsCompleted
                    && e.CompletedAt.HasValue && e.CompletedAt.Value.Hour >= 5 && e.CompletedAt.Value.Hour < 7, ct),

            "journal_keeper" => await _db.DailyLogs
                .CountAsync(d => d.UserId == userId, ct) >= 30,

            _ => false
        };
    }

    private async Task<bool> HasStreakOfAtLeast(Guid userId, int minStreak, CancellationToken ct)
    {
        var habits = await _db.Habits
            .Where(h => h.UserId == userId && !h.IsArchived)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        foreach (var habit in habits)
        {
            var completedDates = await _db.HabitEntries
                .Where(e => e.HabitId == habit.Id && e.IsCompleted)
                .Select(e => e.Date)
                .ToListAsync(ct);

            if (StreakCalculator.Calculate(completedDates, today) >= minStreak)
                return true;
        }

        return false;
    }

    private async Task<bool> HasPerfectWeek(Guid userId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Must have at least one active habit
        var hasActiveHabit = await _db.Habits
            .AnyAsync(h => h.UserId == userId && !h.IsArchived, ct);
        if (!hasActiveHabit) return false;

        // Check the last 7 consecutive days (including today)
        for (int i = 0; i < 7; i++)
        {
            var date = today.AddDays(-i);
            var hasCompletion = await _db.HabitEntries
                .AnyAsync(e => e.Habit.UserId == userId && e.Date == date && e.IsCompleted, ct);
            if (!hasCompletion) return false;
        }

        return true;
    }

    private async Task<bool> HasDistinctDaysWithCompletions(Guid userId, DateOnly start, DateOnly end, int minDays, CancellationToken ct)
    {
        var distinctDays = await _db.HabitEntries
            .Where(e => e.Habit.UserId == userId && e.IsCompleted && e.Date >= start && e.Date <= end)
            .Select(e => e.Date)
            .Distinct()
            .CountAsync(ct);

        return distinctDays >= minDays;
    }

    private async Task<bool> HasReachedLevel(Guid userId, int minLevel, CancellationToken ct)
    {
        var userXp = await _db.UserXps
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (userXp is null) return false;

        return LevelCalculator.CalculateLevel(userXp.TotalXp) >= minLevel;
    }

    private static DateOnly GetStartOfWeek()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var diff = ((int)today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return today.AddDays(-diff);
    }

    private static DateOnly GetStartOfMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new DateOnly(today.Year, today.Month, 1);
    }
}
