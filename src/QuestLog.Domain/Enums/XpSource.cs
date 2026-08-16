namespace QuestLog.Domain.Enums;

/// <summary>
/// Identifies the action that awarded XP. Stored on <see cref="QuestLog.Domain.Entities.XpTransaction"/>
/// so we can audit awards and prevent duplicates (idempotency).
/// </summary>
public enum XpSource
{
    HabitCompletion,
    StreakMilestone,
    GoalCompletion,
    GoalMilestone,
    DailyLog
}