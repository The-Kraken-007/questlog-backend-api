namespace QuestLog.Application.Goals.DTOs;

public class MilestoneDto
{
    public int Id { get; set; }
    public int GoalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int SortOrder { get; set; }
    public DateTime? CompletedAt { get; set; }
}
