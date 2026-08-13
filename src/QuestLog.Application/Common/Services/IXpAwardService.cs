using QuestLog.Domain.Enums;

namespace QuestLog.Application.Common.Services;

/// <summary>
/// Abstraction over <see cref="XpAwardService"/> so command handlers can be
/// unit-tested without a real database. Implementations enforce idempotency,
/// log an <see cref="QuestLog.Domain.Entities.XpTransaction"/>, and recompute
/// the user's level via <see cref="LevelCalculator"/>.
/// </summary>
public interface IXpAwardService
{
    Task<XpAwardResult> AwardXpAsync(
        Guid userId,
        int xpAmount,
        XpSource source,
        string? referenceId = null,
        CancellationToken cancellationToken = default);
}