namespace QuestLog.Application.Habits.DTOs;

/// <summary>
/// Response shape returned for a single habit, including streak and today's status.
/// </summary>
public class HabitDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Whether the habit has been completed for today's date.</summary>
    public bool IsCompletedToday { get; set; }

    /// <summary>Number of consecutive days the habit has been completed (ending today or yesterday).</summary>
    public int CurrentStreak { get; set; }
}
