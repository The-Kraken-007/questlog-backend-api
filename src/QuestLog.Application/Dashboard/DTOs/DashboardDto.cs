using QuestLog.Domain.Enums;

namespace QuestLog.Application.Dashboard.DTOs;

public class DashboardDto
{
    public TodayHabitsDto TodayHabits { get; set; } = new();
    public List<StreakDto> TopStreaks { get; set; } = [];
    public List<ActiveGoalDto> ActiveGoals { get; set; } = [];
    public string? TodayLog { get; set; }
    public List<DashboardTaskDto> UpcomingTasks { get; set; } = [];
}

public class TodayHabitsDto
{
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
    public List<HabitSummaryDto> Habits { get; set; } = [];
}

public class HabitSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Emoji { get; set; }
    public bool IsCompletedToday { get; set; }
    public int CurrentStreak { get; set; }
}

public class StreakDto
{
    public int HabitId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Emoji { get; set; }
    public int CurrentStreak { get; set; }
}

public class ActiveGoalDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public GoalStatus Status { get; set; }
    public int ProgressPercent { get; set; }
    public int TotalMilestones { get; set; }
    public int CompletedMilestones { get; set; }
}

public class DashboardTaskDto
{
    public int Id { get; set; }
    public int QuestTaskListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public string ListName { get; set; } = string.Empty;
}
