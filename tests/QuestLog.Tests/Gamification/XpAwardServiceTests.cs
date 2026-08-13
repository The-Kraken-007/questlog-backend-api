using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Gamification;

/// <summary>
/// Exercises <see cref="XpAwardService"/> against an EF Core in-memory database
/// (via <see cref="TestDbContext"/>) to verify XP awarding, level recalculation,
/// idempotency, and transaction logging.
/// </summary>
public class XpAwardServiceTests
{
    private readonly TestDbContext _db;
    private readonly XpAwardService _sut;

    public XpAwardServiceTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new TestDbContext(options);
        _db.Database.EnsureCreated();

        // TestDbContext does NOT apply AppDbContext's query filters, so we can
        // write rows for any userId directly without tripping the current-user scope.
        var logger = new LoggerFactory().CreateLogger<XpAwardService>();
        _sut = new XpAwardService(_db, logger);
    }

    private static readonly Guid UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task AwardXp_CreatesUserXpRow_AndLogsTransaction_WhenFirstAward()
    {
        var result = await _sut.AwardXpAsync(UserId, 50, XpSource.GoalCompletion, "goal-1");

        result.XpAwarded.ShouldBe(50);
        result.NewLevel.ShouldBe(1);           // 50 < 100 needed for level 2 → level 1
        result.LevelUp.ShouldBeFalse();
        result.PreviousLevel.ShouldBe(1);      // newly-created row defaults to level 1

        var userXp = await _db.UserXps.SingleAsync(x => x.UserId == UserId);
        userXp.TotalXp.ShouldBe(50);
        userXp.CurrentLevel.ShouldBe(1);

        var txn = await _db.XpTransactions.SingleAsync(t => t.UserId == UserId);
        txn.Amount.ShouldBe(50);
        txn.Source.ShouldBe(XpSource.GoalCompletion);
        txn.ReferenceId.ShouldBe("goal-1");
    }

    [Fact]
    public async Task AwardXp_UpdatesExistingUserXp_WhenUserAlreadyHasXp()
    {
        // Seed an existing UserXp row
        _db.UserXps.Add(new UserXp { UserId = UserId, TotalXp = 80, CurrentLevel = 1, UpdatedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();

        var result = await _sut.AwardXpAsync(UserId, 30, XpSource.HabitCompletion, "habit-1_2026-01-01");

        result.XpAwarded.ShouldBe(30);
        // 80 + 30 = 110 XP → level 2 (needs 100 for level 2 from level 1)
        result.NewLevel.ShouldBe(2);
        result.LevelUp.ShouldBeTrue();
        result.PreviousLevel.ShouldBe(1);

        var userXp = await _db.UserXps.SingleAsync(x => x.UserId == UserId);
        userXp.TotalXp.ShouldBe(110);
        userXp.CurrentLevel.ShouldBe(2);
    }

    [Fact]
    public async Task AwardXp_SkipsDuplicate_HabitCompletion_SameDay()
    {
        // First award succeeds — reference id includes the date
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await _sut.AwardXpAsync(UserId, 10, XpSource.HabitCompletion, $"1_{today}");

        // Second award for same habit same day → idempotent skip
        var result = await _sut.AwardXpAsync(UserId, 10, XpSource.HabitCompletion, $"1_{today}");

        result.XpAwarded.ShouldBe(0);
        result.Idempotent.ShouldBeTrue();

        // Only one transaction should exist
        var txns = await _db.XpTransactions.Where(t => t.UserId == UserId).ToListAsync();
        txns.Count.ShouldBe(1);
    }

    [Fact]
    public async Task AwardXp_AllowsHabitCompletion_NextDay()
    {
        // Award today — reference id includes today's date
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        await _sut.AwardXpAsync(UserId, 10, XpSource.HabitCompletion, $"1_{today}");

        // Award for yesterday — different reference id → allowed
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var result = await _sut.AwardXpAsync(UserId, 10, XpSource.HabitCompletion, $"1_{yesterday}");

        result.XpAwarded.ShouldBe(10);
        result.Idempotent.ShouldBeFalse();

        var txns = await _db.XpTransactions.Where(t => t.UserId == UserId).ToListAsync();
        txns.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AwardXp_SkipsDuplicate_GoalCompletion()
    {
        await _sut.AwardXpAsync(UserId, 100, XpSource.GoalCompletion, "goal-5");

        var second = await _sut.AwardXpAsync(UserId, 100, XpSource.GoalCompletion, "goal-5");

        second.XpAwarded.ShouldBe(0);
        second.Idempotent.ShouldBeTrue();

        // But completing a DIFFERENT goal is allowed
        var differentGoal = await _sut.AwardXpAsync(UserId, 100, XpSource.GoalCompletion, "goal-6");
        differentGoal.XpAwarded.ShouldBe(100);
        differentGoal.Idempotent.ShouldBeFalse();
    }

    [Fact]
    public async Task AwardXp_SkipsDuplicate_DailyLog()
    {
        await _sut.AwardXpAsync(UserId, 15, XpSource.DailyLog, "2025-01-01");

        var second = await _sut.AwardXpAsync(UserId, 15, XpSource.DailyLog, "2025-01-01");

        second.XpAwarded.ShouldBe(0);
        second.Idempotent.ShouldBeTrue();

        // A different date is allowed
        var otherDate = await _sut.AwardXpAsync(UserId, 15, XpSource.DailyLog, "2025-01-02");
        otherDate.XpAwarded.ShouldBe(15);
    }

    [Fact]
    public async Task AwardXp_SkipsDuplicate_StreakMilestone()
    {
        // Streak milestone is once-per-habit-per-threshold (idempotent per ref+source, no day constraint)
        await _sut.AwardXpAsync(UserId, 50, XpSource.StreakMilestone, "habit-1");

        var second = await _sut.AwardXpAsync(UserId, 50, XpSource.StreakMilestone, "habit-1");

        second.XpAwarded.ShouldBe(0);
        second.Idempotent.ShouldBeTrue();
    }

    [Fact]
    public async Task AwardXp_ReturnsZeroAndNoSave_WhenAmountIsZero()
    {
        var result = await _sut.AwardXpAsync(UserId, 0, XpSource.HabitCompletion, "habit-1_2026-01-01");

        result.XpAwarded.ShouldBe(0);
        result.Idempotent.ShouldBeFalse();
        result.NewLevel.ShouldBeNull();

        // No transaction logged
        (await _db.XpTransactions.AnyAsync(t => t.UserId == UserId)).ShouldBeFalse();
    }

    [Fact]
    public async Task AwardXp_LevelUp_DetectsMultipleLevelUps()
    {
        // Cumulative to level 5 = 1701 XP. Awarding 2000 XP → jump from level 1 to level 5.
        var result = await _sut.AwardXpAsync(UserId, 2000, XpSource.GoalCompletion, "test-goal");

        result.XpAwarded.ShouldBe(2000);
        result.LevelUp.ShouldBeTrue();
        result.NewLevel.ShouldNotBeNull();
        result.NewLevel.Value.ShouldBeGreaterThan(2);
    }
}
