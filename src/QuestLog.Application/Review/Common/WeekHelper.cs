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
}
