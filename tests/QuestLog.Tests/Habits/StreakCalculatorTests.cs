using Shouldly;
using QuestLog.Application.Habits.Common;
using Xunit;

namespace QuestLog.Tests.Habits;

public class StreakCalculatorTests
{
    private static DateOnly Today => new DateOnly(2026, 7, 12);

    [Fact]
    public void Calculate_ReturnsZero_WhenNoDatesCompleted()
    {
        var result = StreakCalculator.Calculate([], Today);
        result.ShouldBe(0);
    }

    [Fact]
    public void Calculate_ReturnsOne_WhenOnlyTodayCompleted()
    {
        var dates = new[] { Today };
        var result = StreakCalculator.Calculate(dates, Today);
        result.ShouldBe(1);
    }

    [Fact]
    public void Calculate_ReturnsOne_WhenOnlyYesterdayCompleted()
    {
        var yesterday = Today.AddDays(-1);
        var dates = new[] { yesterday };
        var result = StreakCalculator.Calculate(dates, Today);
        result.ShouldBe(1);
    }

    [Fact]
    public void Calculate_ReturnsZero_WhenLastCompletionWasTwoDaysAgo()
    {
        var twoDaysAgo = Today.AddDays(-2);
        var dates = new[] { twoDaysAgo };
        var result = StreakCalculator.Calculate(dates, Today);
        result.ShouldBe(0);
    }

    [Fact]
    public void Calculate_ReturnsFive_WhenFiveDaysConsecutiveEndingToday()
    {
        var dates = Enumerable.Range(0, 5).Select(i => Today.AddDays(-i));
        var result = StreakCalculator.Calculate(dates, Today);
        result.ShouldBe(5);
    }

    [Fact]
    public void Calculate_StopsAtGap_WhenConsecutiveRunHasBreak()
    {
        // 3 days, gap, then 2 more days. Streak should be 3 (ending today).
        var dates = new[]
        {
            Today,
            Today.AddDays(-1),
            Today.AddDays(-2),
            // gap at -3
            Today.AddDays(-4),
            Today.AddDays(-5),
        };
        var result = StreakCalculator.Calculate(dates, Today);
        result.ShouldBe(3);
    }

    [Fact]
    public void Calculate_CountsFromYesterday_WhenTodayNotYetCompleted()
    {
        // Today not done, but 4 consecutive days ending yesterday → streak = 4
        var dates = Enumerable.Range(1, 4).Select(i => Today.AddDays(-i));
        var result = StreakCalculator.Calculate(dates, Today);
        result.ShouldBe(4);
    }
}
