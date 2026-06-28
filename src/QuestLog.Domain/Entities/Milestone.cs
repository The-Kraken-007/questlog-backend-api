namespace QuestLog.Domain.Entities;

public class Milestone
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public int SortOrder { get; set; } = 0;
    public DateTime? CompletedAt { get; set; }

    public Goal Goal { get; set; } = null!;
}
