using QuestLog.Domain.Enums;

namespace QuestLog.Domain.Entities;

/// <summary>
/// Immutable audit record of a single XP award. One row per award call.
/// Used by <c>XpAwardService</c> to enforce idempotency (no duplicate XP for
/// the same action on the same day / for the same reference id).
/// </summary>
public class XpTransaction
{
    public int Id { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>XP amount awarded (always positive).</summary>
    public int Amount { get; set; }

    public XpSource Source { get; set; }

    /// <summary>
    /// Stable string key identifying the source action:
    /// — HabitCompletion / StreakMilestone: habit id
    /// — GoalCompletion: goal id
    /// — GoalMilestone: milestone id
    /// — DailyLog: yyyy-MM-dd date string
    /// Used together with <see cref="Source"/> to detect duplicate awards.
    /// </summary>
    public string? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}