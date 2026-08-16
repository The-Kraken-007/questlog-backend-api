namespace QuestLog.Application.Review.DTOs;

/// <summary>
/// Aggregated summary of a single calendar week for the weekly review page.
/// The empty week case (no data at all) is returned as 200 with all-zero defaults
/// so the frontend can render the review scaffolding without special-casing 404.
/// </summary>
public class WeeklyReviewDto
{
    public DateOnly WeekStart { get; set; }
    public DateOnly WeekEnd { get; set; }

    public XpEarnedDto XpEarned { get; set; } = new();
    public LevelChangeDto LevelChange { get; set; } = new();
    public HabitSummaryDto Habits { get; set; } = new();
    public GoalSummaryDto Goals { get; set; } = new();
    public DailyLogSummaryDto DailyLogs { get; set; } = new();
    public List<AchievementSummaryDto> Achievements { get; set; } = new();
    public ReflectionDto Reflection { get; set; } = new();
}

public class XpEarnedDto
{
    public int Total { get; set; }
    public List<DailyXpDto> Daily { get; set; } = new();
}

public class DailyXpDto
{
    public DateOnly Date { get; set; }
    public int Amount { get; set; }
}

public class LevelChangeDto
{
    public int From { get; set; }
    public int To { get; set; }
}

public class HabitSummaryDto
{
    /// <summary>Number of active (non-archived) habits.</summary>
    public int Total { get; set; }

    /// <summary>Number of completed habit entries during the week.</summary>
    public int Completed { get; set; }

    /// <summary>Completion rate 0-100: completed / (active habits × 7 days).</summary>
    public double Rate { get; set; }

    /// <summary>Longest consecutive-day completion run ending anywhere inside the week.</summary>
    public int BestStreak { get; set; }

    /// <summary>One entry per day of the week — drives the heatmap.</summary>
    public List<HabitDailyBreakdownDto> DailyBreakdown { get; set; } = new();
}

public class HabitDailyBreakdownDto
{
    public DateOnly Date { get; set; }
    public int Completed { get; set; }
    public int Total { get; set; }
    public double Rate { get; set; }
}

public class GoalSummaryDto
{
    public int Active { get; set; }
    public int Completed { get; set; }
    public int MilestonesCompleted { get; set; }
}

public class DailyLogSummaryDto
{
    public int DaysLogged { get; set; }
    public int WordCount { get; set; }
}

public class AchievementSummaryDto
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public DateTime UnlockedAt { get; set; }
}

public class ReflectionDto
{
    public string? Notes { get; set; }
    public bool Exists { get; set; }
}
