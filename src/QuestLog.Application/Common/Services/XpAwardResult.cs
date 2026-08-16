namespace QuestLog.Application.Common.Services;

/// <summary>
/// Outcome of a single <see cref="XpAwardService.AwardXpAsync"/> call. The
/// handler aggregates one or more of these (e.g. habit completion + streak
/// milestone) to populate a <see cref="QuestLog.Application.Common.DTOs.GamifiedResult{T}"/>.
/// </summary>
public record XpAwardResult(
    int XpAwarded,
    int? PreviousLevel,
    int? NewLevel,
    bool LevelUp,
    bool Idempotent)
{
    public static readonly XpAwardResult Skipped = new(0, null, null, false, true);
}