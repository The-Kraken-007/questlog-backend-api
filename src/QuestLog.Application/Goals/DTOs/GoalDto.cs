using QuestLog.Domain.Enums;

namespace QuestLog.Application.Goals.DTOs;

public class GoalDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly? TargetDate { get; set; }
    public GoalStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Percentage of milestones completed (0-100).
    /// 0 if the goal has no milestones yet.
    /// </summary>
    public int ProgressPercent { get; set; }

    public List<MilestoneDto> Milestones { get; set; } = [];
}
