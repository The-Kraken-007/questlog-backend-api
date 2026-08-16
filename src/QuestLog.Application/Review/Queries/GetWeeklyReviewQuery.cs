using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.Review.Common;
using QuestLog.Application.Review.DTOs;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Review.Queries;

/// <summary>
/// Fetches the aggregated summary for one calendar week: XP earned, level
/// change, habit completion heatmap, goal progress, daily log stats,
/// achievements unlocked, and the saved reflection note.
///
/// A week with no data is NOT an error — it returns 200 with zero defaults so
/// the review page renders scaffolding instead of a 404.
/// </summary>
public record GetWeeklyReviewQuery(DateOnly WeekStart) : IRequest<WeeklyReviewDto>;

public class GetWeeklyReviewQueryHandler : IRequestHandler<GetWeeklyReviewQuery, WeeklyReviewDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetWeeklyReviewQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<WeeklyReviewDto> Handle(GetWeeklyReviewQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var weekStart = WeekHelper.StartOfWeek(request.WeekStart);
        var weekEnd = weekStart.AddDays(6);

        // UTC boundaries for timestamp comparisons (XpTransaction, CompletedAt, UnlockedAt).
        // Kind must be Utc — Npgsql rejects Unspecified against timestamptz columns.
        var weekStartUtc = WeekHelper.UtcDayStart(weekStart);
        var weekEndExclusiveUtc = WeekHelper.UtcDayStart(weekEnd.AddDays(1));

        // ── XP earned this week (audit log, scoped to user via query filter) ──
        var xpTransactions = await _db.XpTransactions
            .Where(x => x.CreatedAt >= weekStartUtc && x.CreatedAt < weekEndExclusiveUtc)
            .ToListAsync(ct);

        var xpDaily = xpTransactions
            .GroupBy(x => DateOnly.FromDateTime(x.CreatedAt))
            .Select(g => new DailyXpDto { Date = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderBy(d => d.Date)
            .ToList();

        // ── Level change: level before the week vs. current level ─────────────
        // XpTransaction is an append-only audit, so TotalXp = sum of all awards.
        // XP earned before the week = current total − XP earned this week.
        var userXp = await _db.UserXps.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        var currentTotalXp = userXp?.TotalXp ?? 0;
        var xpTotal = xpTransactions.Sum(x => x.Amount);

        // ── Habits: completions scoped via the user-filtered Habit query ──────
        var habits = await _db.Habits
            .Where(h => !h.IsArchived)
            .Include(h => h.Entries)
            .ToListAsync(ct);

        var weekEntries = habits
            .SelectMany(h => h.Entries)
            .Where(e => e.IsCompleted && e.Date >= weekStart && e.Date <= weekEnd)
            .ToList();

        var activeHabitCount = habits.Count;
        var completedCount = weekEntries.Count;
        double rate = activeHabitCount == 0
            ? 0
            : Math.Round(completedCount / (double)(activeHabitCount * 7) * 100, 1);

        var dailyBreakdown = new List<HabitDailyBreakdownDto>();
        for (int i = 0; i < 7; i++)
        {
            var day = weekStart.AddDays(i);
            var completedThatDay = weekEntries.Count(e => e.Date == day);
            dailyBreakdown.Add(new HabitDailyBreakdownDto
            {
                Date        = day,
                Completed   = completedThatDay,
                Total       = activeHabitCount,
                Rate        = activeHabitCount == 0
                    ? 0
                    : Math.Round(completedThatDay / (double)activeHabitCount * 100, 1)
            });
        }

        // Best streak: longest run of consecutive completed days per habit
        // inside the week window (dates are de-duplicated per habit).
        int bestStreak = 0;
        foreach (var group in weekEntries.GroupBy(e => e.HabitId))
        {
            var dates = group.Select(e => e.Date).Distinct().OrderBy(d => d).ToList();
            if (dates.Count == 0)
                continue;

            int run = 1;
            bestStreak = Math.Max(bestStreak, run);
            for (int i = 1; i < dates.Count; i++)
            {
                run = dates[i].DayNumber == dates[i - 1].DayNumber + 1 ? run + 1 : 1;
                bestStreak = Math.Max(bestStreak, run);
            }
        }

        // ── Goals + milestones scoped via the user-filtered Goal query ────────
        var goals = await _db.Goals
            .Include(g => g.Milestones)
            .ToListAsync(ct);

        var milestonesCompleted = goals
            .SelectMany(g => g.Milestones)
            .Count(m => m.IsCompleted
                        && m.CompletedAt >= weekStartUtc
                        && m.CompletedAt < weekEndExclusiveUtc);

        // ── Daily logs ────────────────────────────────────────────────────────
        var logs = await _db.DailyLogs
            .Where(d => d.Date >= weekStart && d.Date <= weekEnd)
            .ToListAsync(ct);

        // ── Achievements unlocked this week ───────────────────────────────────
        var achievements = await _db.UserAchievements
            .Where(ua => ua.UnlockedAt >= weekStartUtc && ua.UnlockedAt < weekEndExclusiveUtc)
            .Include(ua => ua.Achievement)
            .ToListAsync(ct);

        // ── Reflection note (upserted separately via the command) ─────────────
        var reflection = await _db.WeeklyReflections
            .FirstOrDefaultAsync(w => w.WeekStart == weekStart, ct);

        return new WeeklyReviewDto
        {
            WeekStart  = weekStart,
            WeekEnd    = weekEnd,
            XpEarned   = new XpEarnedDto { Total = xpTotal, Daily = xpDaily },
            LevelChange = new LevelChangeDto
            {
                From = LevelCalculator.CalculateLevel(Math.Max(0, currentTotalXp - xpTotal)),
                To   = LevelCalculator.CalculateLevel(currentTotalXp)
            },
            Habits = new HabitSummaryDto
            {
                Total          = activeHabitCount,
                Completed      = completedCount,
                Rate           = rate,
                BestStreak     = bestStreak,
                DailyBreakdown = dailyBreakdown
            },
            Goals = new GoalSummaryDto
            {
                Active              = goals.Count(g => g.Status == GoalStatus.Active),
                Completed           = goals.Count(g => g.CompletedAt >= weekStartUtc && g.CompletedAt < weekEndExclusiveUtc),
                MilestonesCompleted = milestonesCompleted
            },
            DailyLogs = new DailyLogSummaryDto
            {
                DaysLogged = logs.Count,
                WordCount  = logs.Sum(l => l.Content.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length)
            },
            Achievements = achievements
                .Select(ua => new AchievementSummaryDto
                {
                    Key        = ua.Achievement.Key,
                    Name       = ua.Achievement.Name,
                    Icon       = ua.Achievement.Icon,
                    UnlockedAt = ua.UnlockedAt
                })
                .ToList(),
            Reflection = new ReflectionDto
            {
                Notes  = reflection?.Notes,
                Exists = reflection is not null
            }
        };
    }
}
