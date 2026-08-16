using QuestLog.Domain.Entities;

namespace QuestLog.Application.Common.Services;

/// <summary>
/// Abstraction over <see cref="AchievementChecker"/> so command handlers can
/// be unit-tested without a real database. Implementations evaluate all
/// achievement conditions and persist any newly unlocked ones.
/// </summary>
public interface IAchievementChecker
{
    Task<List<Achievement>> CheckAndUnlockAsync(CancellationToken ct = default);
}