using QuestLog.Application.Goals.DTOs;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Goals.Common;

/// <summary>
/// Maps a fully-loaded Goal entity (with Milestones) to a GoalDto.
/// Centralised here so every query/command returns consistent data.
/// </summary>
public static class GoalMapper
{
    public static GoalDto ToDto(Goal goal)
    {
        int total = goal.Milestones.Count;
        int done = goal.Milestones.Count(m => m.IsCompleted);

        // Avoid division by zero; 0 milestones → 0%
        int progress = total > 0 ? (int)Math.Round(done / (double)total * 100) : 0;

        return new GoalDto
        {
            Id             = goal.Id,
            Title          = goal.Title,
            Description    = goal.Description,
            TargetDate     = goal.TargetDate,
            Status         = goal.Status,
            CreatedAt      = goal.CreatedAt,
            CompletedAt    = goal.CompletedAt,
            ProgressPercent = progress,
            Milestones     = goal.Milestones
                .OrderBy(m => m.SortOrder)
                .Select(m => new MilestoneDto
                {
                    Id          = m.Id,
                    GoalId      = m.GoalId,
                    Title       = m.Title,
                    IsCompleted = m.IsCompleted,
                    SortOrder   = m.SortOrder,
                    CompletedAt = m.CompletedAt
                }).ToList()
        };
    }
}
