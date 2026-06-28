namespace QuestLog.Application.Habits.DTOs;

/// <summary>
/// Response shape for a single habit entry, used to populate the calendar heatmap.
/// </summary>
public class HabitEntryDto
{
    public int Id { get; set; }
    public int HabitId { get; set; }

    /// <summary>Date of this entry in ISO 8601 format (yyyy-MM-dd).</summary>
    public DateOnly Date { get; set; }

    public bool IsCompleted { get; set; }
}
