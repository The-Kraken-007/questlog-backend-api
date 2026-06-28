namespace QuestLog.Application.Habits.Common;

/// <summary>
/// Pure static helper for computing habit streak from a set of completion dates.
/// Lives in the Application layer with no external dependencies.
/// </summary>
public static class StreakCalculator
{
    /// <summary>
    /// Calculates the current streak given a set of dates on which the habit was completed.
    /// 
    /// Rules:
    /// - A streak is the longest consecutive-day run ending on today or yesterday.
    /// - If today is not completed yet, the streak counts backwards from yesterday.
    /// - If neither today nor yesterday is completed, the streak is 0.
    /// </summary>
    /// <param name="completedDates">The set of dates the habit was marked as completed.</param>
    /// <param name="today">Today's date. Passed in so this function is pure/testable.</param>
    /// <returns>The current streak length in days.</returns>
    public static int Calculate(IEnumerable<DateOnly> completedDates, DateOnly today)
    {
        var dateSet = new HashSet<DateOnly>(completedDates);

        // Determine anchor: start from today if completed, else from yesterday
        DateOnly anchor = dateSet.Contains(today) ? today : today.AddDays(-1);

        // If anchor date itself wasn't completed, streak is 0
        if (!dateSet.Contains(anchor))
            return 0;

        int streak = 0;
        DateOnly current = anchor;

        while (dateSet.Contains(current))
        {
            streak++;
            current = current.AddDays(-1);
        }

        return streak;
    }
}
