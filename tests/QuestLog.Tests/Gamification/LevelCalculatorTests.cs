using QuestLog.Application.Common.Services;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Gamification;

public class LevelCalculatorTests
{
    // ── CalculateLevel ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(381, 2)]
    [InlineData(382, 3)]
    [InlineData(901, 4)]
    [InlineData(1701, 5)]
    [InlineData(11102, 10)]
    [InlineData(14264, 11)]
    public void CalculateLevel_ReturnsCorrectLevel(int totalXp, int expectedLevel)
    {
        LevelCalculator.CalculateLevel(totalXp).ShouldBe(expectedLevel);
    }

    // ── GetTitle ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, "Novice")]
    [InlineData(4, "Novice")]
    [InlineData(5, "Apprentice")]
    [InlineData(9, "Apprentice")]
    [InlineData(10, "Adept")]
    [InlineData(19, "Adept")]
    [InlineData(20, "Specialist")]
    [InlineData(29, "Specialist")]
    [InlineData(30, "Expert")]
    [InlineData(39, "Expert")]
    [InlineData(40, "Master")]
    [InlineData(49, "Master")]
    [InlineData(50, "Grandmaster")]
    [InlineData(74, "Grandmaster")]
    [InlineData(75, "Champion")]
    [InlineData(99, "Champion")]
    [InlineData(100, "Mythic")]
    [InlineData(150, "Mythic")]
    public void GetTitle_ReturnsCorrectTitle(int level, string expectedTitle)
    {
        LevelCalculator.GetTitle(level).ShouldBe(expectedTitle);
    }

    // ── GetXpForLevel ───────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 100)]    // floor(100 * 1^1.5) = 100
    [InlineData(2, 282)]    // floor(100 * 2^1.5) = 282
    [InlineData(3, 519)]    // floor(100 * 3^1.5) = 519
    [InlineData(4, 800)]    // floor(100 * 4^1.5) = 800
    [InlineData(5, 1118)]   // floor(100 * 5^1.5) = 1118
    [InlineData(10, 3162)]  // floor(100 * 10^1.5) = 3162
    public void GetXpForLevel_ReturnsCorrectXp(int level, int expectedXp)
    {
        LevelCalculator.GetXpForLevel(level).ShouldBe(expectedXp);
    }

    // ── GetTotalXpForLevel ──────────────────────────────────────────────

    [Theory]
    [InlineData(1, 0)]        // Level 1 starts at 0
    [InlineData(2, 100)]      // 100
    [InlineData(3, 382)]      // 100 + 282
    [InlineData(4, 901)]      // 100 + 282 + 519
    [InlineData(5, 1701)]     // 100 + 282 + 519 + 800
    [InlineData(10, 11102)]   // Sum of levels 1-9
    public void GetTotalXpForLevel_ReturnsCorrectCumulativeXp(int level, int expectedTotal)
    {
        LevelCalculator.GetTotalXpForLevel(level).ShouldBe(expectedTotal);
    }

    // ── GetXpInCurrentLevel ─────────────────────────────────────────────

    [Theory]
    [InlineData(0, 0)]        // Level 1, 0 XP in
    [InlineData(50, 50)]      // Level 1, 50 XP in
    [InlineData(100, 0)]      // Level 2, 0 XP in
    [InlineData(150, 50)]     // Level 2, 50 XP in
    [InlineData(382, 0)]      // Level 3, 0 XP in
    [InlineData(1000, 99)]    // Level 4, 99 XP in (1000 - 901)
    public void GetXpInCurrentLevel_ReturnsCorrectXp(int totalXp, int expectedXpInLevel)
    {
        LevelCalculator.GetXpInCurrentLevel(totalXp).ShouldBe(expectedXpInLevel);
    }

    // ── GetXpToNextLevel ────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 100)]      // Need 100 to reach level 2
    [InlineData(50, 50)]      // Need 50 more
    [InlineData(100, 282)]    // Need 282 to reach level 3
    [InlineData(382, 519)]    // Need 519 to reach level 4
    [InlineData(1000, 701)]   // Level 4 needs 800, has 99 → 800 - 99 = 701
    public void GetXpToNextLevel_ReturnsCorrectXp(int totalXp, int expectedXpToNext)
    {
        LevelCalculator.GetXpToNextLevel(totalXp).ShouldBe(expectedXpToNext);
    }
}
