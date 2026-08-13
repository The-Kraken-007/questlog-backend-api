using QuestLog.Application.Achievements.DTOs;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Achievements.Common;

/// <summary>
/// Maps <see cref="Achievement"/> entities to the wire DTO. Used by both the
/// achievements query handler and command handlers that emit "newly unlocked"
/// achievements inside their <see cref="Common.DTOs.GamifiedResult{T}"/> response.
/// </summary>
public static class AchievementMapper
{
    public static AchievementDto ToDto(Achievement a, DateTime? unlockedAt = null) => new()
    {
        Key = a.Key,
        Name = a.Name,
        Description = a.Description,
        Category = a.Category,
        Icon = a.Icon,
        Unlocked = true,
        UnlockedAt = unlockedAt ?? DateTime.UtcNow
    };
}