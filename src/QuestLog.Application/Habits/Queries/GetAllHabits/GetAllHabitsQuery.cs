using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.Common;
using QuestLog.Application.Habits.DTOs;

namespace QuestLog.Application.Habits.Queries.GetAllHabits;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all active (non-archived) habits, ordered by SortOrder,
/// each with today's completion status and current streak.
/// </summary>
public record GetAllHabitsQuery : IRequest<List<HabitDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetAllHabitsQueryHandler : IRequestHandler<GetAllHabitsQuery, List<HabitDto>>
{
    private readonly IAppDbContext _db;

    public GetAllHabitsQueryHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<List<HabitDto>> Handle(GetAllHabitsQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var habits = await _db.Habits
            .Where(h => !h.IsArchived)
            .OrderBy(h => h.SortOrder)
            .Include(h => h.Entries)
            .ToListAsync(cancellationToken);

        return habits.Select(habit =>
        {
            var completedDates = habit.Entries
                .Where(e => e.IsCompleted)
                .Select(e => e.Date)
                .ToList();

            bool isCompletedToday = completedDates.Contains(today);
            int streak = StreakCalculator.Calculate(completedDates, today);

            return new HabitDto
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
        }).ToList();
    }
}
