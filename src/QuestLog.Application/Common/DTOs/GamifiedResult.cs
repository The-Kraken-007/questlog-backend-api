using QuestLog.Application.Achievements.DTOs;

namespace QuestLog.Application.Common.DTOs;

/// <summary>
/// Generic envelope returned by mutating command handlers (habit toggle, goal
/// update, milestone toggle, daily log save) that may award XP or unlock
/// achievements. Existing DTO data is carried in <see cref="Data"/> so callers
/// can both update local state and react to gamification events.
/// </summary>
public record GamifiedResult<T>(
    T Data,
    int XpAwarded,
    int? NewLevel,
    bool LevelUp,
    IReadOnlyList<AchievementDto> NewAchievements,
    bool Idempotent)
{
    /// <summary>Builds an empty result (no XP awarded) for actions that did not award.</summary>
    public static GamifiedResult<T> Empty(T data) => new(
        data,
        XpAwarded: 0,
        NewLevel: null,
        LevelUp: false,
        NewAchievements: Array.Empty<AchievementDto>(),
        Idempotent: false);
}