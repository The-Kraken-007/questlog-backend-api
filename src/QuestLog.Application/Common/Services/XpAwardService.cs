using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Common.Services;

/// <summary>
/// Awards XP to a user atomically: logs an <see cref="XpTransaction"/>,
/// updates the <see cref="UserXp"/> aggregate (level recompute), and enforces
/// idempotency so the same action never awards twice.
///
/// Idempotency is enforced at two levels:
///  1. Application: <see cref="IsDuplicateAsync"/> checks for existing transactions.
///  2. Database: a unique composite index on (UserId, Source, ReferenceId) rejects
///     duplicate inserts even under concurrency (TOCTOU protection).
///
/// For HabitCompletion the reference id encodes the date (e.g. "5_2026-08-13")
/// so the same unique index serves daily idempotency without a date-range query.
/// </summary>
public class XpAwardService : IXpAwardService
{
    private readonly IAppDbContext _db;
    private readonly ILogger<XpAwardService> _logger;

    public XpAwardService(IAppDbContext db, ILogger<XpAwardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<XpAwardResult> AwardXpAsync(
        Guid userId,
        int xpAmount,
        XpSource source,
        string? referenceId = null,
        CancellationToken ct = default)
    {
        if (xpAmount <= 0)
            return new XpAwardResult(0, null, null, false, false);

        if (await IsDuplicateAsync(userId, source, referenceId, ct))
        {
            _logger.LogDebug(
                "Skipping duplicate XP award: UserId={UserId}, Source={Source}, ReferenceId={ReferenceId}",
                userId, source, referenceId);
            return XpAwardResult.Skipped;
        }

        // Get or create the user's XP aggregate. (Phase 1 migration seeds zero-XP
        // rows for existing users; new users are seeded on register. The null
        // branch is defensive for tests / race conditions.)
        var userXp = await _db.UserXps.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        int? previousLevel = userXp?.CurrentLevel;

        if (userXp is null)
        {
            userXp = new UserXp
            {
                UserId = userId,
                TotalXp = 0,
                CurrentLevel = 1,
                UpdatedAt = DateTime.UtcNow
            };
            _db.UserXps.Add(userXp);
            previousLevel = 1;
        }

        userXp.TotalXp += xpAmount;
        userXp.CurrentLevel = LevelCalculator.CalculateLevel(userXp.TotalXp);
        userXp.UpdatedAt = DateTime.UtcNow;

        _db.XpTransactions.Add(new XpTransaction
        {
            UserId = userId,
            Amount = xpAmount,
            Source = source,
            ReferenceId = referenceId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Awarded {XpAmount} XP to UserId={UserId} for {Source} {ReferenceId}. Level: {PreviousLevel} → {NewLevel}",
            xpAmount, userId, source, referenceId, previousLevel, userXp.CurrentLevel);

        return new XpAwardResult(
            XpAwarded: xpAmount,
            PreviousLevel: previousLevel,
            NewLevel: userXp.CurrentLevel,
            LevelUp: userXp.CurrentLevel > previousLevel,
            Idempotent: false);
    }

    /// <summary>
    /// Quick application-level duplicate check. The DB unique index is the
    /// authoritative guard; this check just avoids an unnecessary INSERT + exception.
    /// </summary>
    private async Task<bool> IsDuplicateAsync(Guid userId, XpSource source, string? referenceId, CancellationToken ct)
    {
        if (referenceId is null) return false;

        // For HabitCompletion the reference id already encodes the date
        // (e.g. "5_2026-08-13"), so no date-range filter is needed.
        return await _db.XpTransactions
            .AnyAsync(t => t.UserId == userId && t.Source == source && t.ReferenceId == referenceId, ct);
    }
}
