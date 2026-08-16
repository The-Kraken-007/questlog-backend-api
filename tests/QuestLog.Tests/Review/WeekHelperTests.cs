using Shouldly;
using QuestLog.Application.Review.Common;
using Xunit;

namespace QuestLog.Tests.Review;

public class WeekHelperTests
{
    [Fact]
    public void StartOfWeek_KeepsMondayUnchanged()
    {
        WeekHelper.StartOfWeek(new DateOnly(2026, 7, 20)).ShouldBe(new DateOnly(2026, 7, 20));
    }

    [Fact]
    public void StartOfWeek_NormalizesMidweekDateToMonday()
    {
        WeekHelper.StartOfWeek(new DateOnly(2026, 7, 23)).ShouldBe(new DateOnly(2026, 7, 20));
    }

    [Fact]
    public void StartOfWeek_SundayBelongsToWeekThatStartedBeforeIt()
    {
        WeekHelper.StartOfWeek(new DateOnly(2026, 7, 26)).ShouldBe(new DateOnly(2026, 7, 20));
    }

    [Fact]
    public void StartOfWeek_FirstDayOfWeekIsMonday()
    {
        WeekHelper.StartOfWeek(new DateOnly(2026, 1, 1)).ShouldBe(new DateOnly(2025, 12, 29));
    }
}
