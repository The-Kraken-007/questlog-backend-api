using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Common.Services;

/// <summary>
/// Awards XP to a user atomically: logs an <see cref="XpTransaction"/>,
/// updates the <see cref="UserXp"/> aggregate (level recompute), and enforces
/// idempotency so the same action never awards twice.
///
/// Idempotency rules per <see cref="XpSource"/>:
///  - <see cref="XpSource.HabitCompletion"/>: skip if a transaction exists with
///    the same habit id (ReferenceId) and CreatedAt on the same UTC calendar
///    day as now. A user can earn again the next day or after un-completing.
///  - <see cref="XpSource.StreakMilestone"/>: skip if exists for same habit id
///    and same source. Streak milestones are once-per-habit events.
///  - <see cref="XpSource.GoalCompletion"/>: skip if exists for same goal id.
///  - <see cref="XpSource.GoalMilestone"/>: skip if exists for same milestone id.
///  - <see cref="XpSource.DailyLog"/>: skip if exists for same date string.
/// </summary>
public class XpAwardService : IXpAwardService
{
    private readonly IAppDbContext _db;

    public XpAwardService(IAppDbContext db) => _db = db;

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
            return XpAwardResult.Skipped;

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

        return new XpAwardResult(
            XpAwarded: xpAmount,
            PreviousLevel: previousLevel,
            NewLevel: userXp.CurrentLevel,
            LevelUp: userXp.CurrentLevel > previousLevel,
            Idempotent: false);
    }

    private async Task<bool> IsDuplicateAsync(Guid userId, XpSource source, string? referenceId, CancellationToken ct)
    {
        if (referenceId is null) return false;

        var query = _db.XpTransactions
            .Where(t => t.UserId == userId && t.Source == source && t.ReferenceId == referenceId);

        if (source == XpSource.HabitCompletion)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(t => t.CreatedAt.Date == today);
        }

        return await query.AnyAsync(ct);
    }
}