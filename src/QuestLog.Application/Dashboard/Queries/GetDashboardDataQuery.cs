using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Dashboard.DTOs;
using QuestLog.Application.Habits.Common;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Dashboard.Queries;

public record GetDashboardDataQuery : IRequest<DashboardDto>;

public class GetDashboardDataQueryHandler : IRequestHandler<GetDashboardDataQuery, DashboardDto>
{
    private readonly IHabitRepository _habitRepo;
    private readonly IGoalRepository _goalRepo;
    private readonly IDailyLogRepository _logRepo;
    private readonly IAppDbContext _context;

    public GetDashboardDataQueryHandler(
        IHabitRepository habitRepo,
        IGoalRepository goalRepo,
        IDailyLogRepository logRepo,
        IAppDbContext context)
    {
        _habitRepo = habitRepo;
        _goalRepo = goalRepo;
        _logRepo = logRepo;
        _context = context;
    }

    public async Task<DashboardDto> Handle(GetDashboardDataQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Load all active habits with their entries in a single query
        var habits = await _habitRepo.GetAllActiveAsync(cancellationToken);

        // Build habit summaries — reuse the same StreakCalculator from Phase 2
        var habitSummaries = habits.Select(h =>
        {
            var completedDates = h.Entries.Where(e => e.IsCompleted).Select(e => e.Date).ToHashSet();
            return new HabitSummaryDto
            {
                Id               = h.Id,
                Name             = h.Name,
                Emoji            = h.Emoji,
                IsCompletedToday = completedDates.Contains(today),
                CurrentStreak    = StreakCalculator.Calculate(completedDates, today)
            };
        }).ToList();

        // Top 3 streaks — only habits that have an active streak
        var topStreaks = habitSummaries
            .Where(h => h.CurrentStreak > 0)
            .OrderByDescending(h => h.CurrentStreak)
            .Take(3)
            .Select(h => new StreakDto
            {
                HabitId       = h.Id,
                Name          = h.Name,
                Emoji         = h.Emoji,
                CurrentStreak = h.CurrentStreak
            }).ToList();

        var activeGoals = await _goalRepo.GetAllActiveAsync(cancellationToken);

        var activeGoalDtos = activeGoals.Select(g =>
        {
            int total    = g.Milestones.Count;
            int done     = g.Milestones.Count(m => m.IsCompleted);
            int progress = total > 0 ? (int)Math.Round(done / (double)total * 100) : 0;

            return new ActiveGoalDto
            {
                Id                   = g.Id,
                Title                = g.Title,
                Status               = g.Status,
                ProgressPercent      = progress,
                TotalMilestones      = total,
                CompletedMilestones  = done
            };
        }).ToList();

        // Today's log — just the content, null if not written yet
        var todayLogEntity = await _logRepo.GetByDateAsync(today, cancellationToken);
        var todayLog = todayLogEntity?.Content;

        // Upcoming Tasks (Today, Tomorrow, Overdue)
        var tomorrow = today.AddDays(1);
        var tomorrowDateTime = tomorrow.ToDateTime(TimeOnly.MaxValue);
        
        var tasks = await _context.QuestTasks
            .Include(t => t.QuestTaskList)
            .Where(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value <= tomorrowDateTime)
            .ToListAsync(cancellationToken);

        var sortedTasks = tasks
            .OrderBy(t => t.DueDate!.Value.Date >= today.ToDateTime(TimeOnly.MinValue) ? 1 : 0) // Overdue first
            .ThenBy(t => t.DueDate)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new DashboardTaskDto
            {
                Id = t.Id,
                QuestTaskListId = t.QuestTaskListId,
                Name = t.Name,
                DueDate = t.DueDate,
                IsCompleted = t.IsCompleted,
                ListName = t.QuestTaskList.Name
            }).ToList();

        return new DashboardDto
        {
            TodayHabits = new TodayHabitsDto
            {
                TotalCount     = habitSummaries.Count,
                CompletedCount = habitSummaries.Count(h => h.IsCompletedToday),
                Habits         = habitSummaries
            },
            TopStreaks  = topStreaks,
            ActiveGoals = activeGoalDtos,
            TodayLog    = todayLog,
            UpcomingTasks = sortedTasks
        };
    }
}
