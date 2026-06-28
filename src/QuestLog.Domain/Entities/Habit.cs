namespace QuestLog.Domain.Entities;

public class Habit
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Emoji { get; set; } = "✅";
    public bool IsArchived { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<HabitEntry> Entries { get; set; } = new List<HabitEntry>();
}
