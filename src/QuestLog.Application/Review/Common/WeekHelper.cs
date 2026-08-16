namespace QuestLog.Application.Review.Common;

/// <summary>
/// Calendar-week helpers. A "week" is always Monday–Sunday; any date in the
/// week normalizes to its Monday so the frontend can pass whatever day it holds.
/// </summary>
public static class WeekHelper
{
    /// <summary>Returns the Monday of the week containing the given date.</summary>
    public static DateOnly StartOfWeek(DateOnly date)
        => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));

    /// <summary>
    /// Converts a date to a UTC midnight boundary. The Kind matters: PostgreSQL
    /// 'timestamptz' columns reject DateTime values with Kind=Unspecified, so
    /// comparisons against UtcNow-written timestamps must use Utc-kind values.
    /// </summary>
    public static DateTime UtcDayStart(DateOnly date)
        => DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
}
