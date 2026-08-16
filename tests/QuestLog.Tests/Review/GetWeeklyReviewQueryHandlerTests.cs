using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Review.Queries;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Review;

public class GetWeeklyReviewQueryHandlerTests : IDisposable
{
    private readonly TestDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly GetWeeklyReviewQueryHandler _sut;
    private readonly Guid _userId;

    // Week under test: Mon 2026-07-20 .. Sun 2026-07-26
    private static readonly DateOnly Mon = new(2026, 7, 20);
    private static readonly DateOnly Tue = new(2026, 7, 21);
    private static readonly DateOnly Wed = new(2026, 7, 22);
    private static readonly DateOnly Thu = new(2026, 7, 23);
    private static readonly DateOnly Fri = new(2026, 7, 24);
    private static readonly DateOnly Sat = new(2026, 7, 25);
    private static readonly DateOnly Sun = new(2026, 7, 26);

    public GetWeeklyReviewQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);
        _userId = Guid.NewGuid();
        _currentUser = Substitute.For<ICurrentUserService>();
        _currentUser.UserId.Returns(_userId);
        _sut = new GetWeeklyReviewQueryHandler(_db, _currentUser);
    }

    [Fact]
    public async Task Handle_EmptyWeek_ReturnsDefaults()
    {
        // Act
        var result = await _sut.Handle(new GetWeeklyReviewQuery(Mon), CancellationToken.None);

        // Assert
        result.WeekStart.ShouldBe(Mon);
        result.WeekEnd.ShouldBe(Sun);
        result.XpEarned.Total.ShouldBe(0);
        result.XpEarned.Daily.ShouldBeEmpty();
        result.LevelChange.From.ShouldBe(1);
        result.LevelChange.To.ShouldBe(1);
        result.Habits.Total.ShouldBe(0);
        result.Habits.Completed.ShouldBe(0);
        result.Habits.Rate.ShouldBe(0);
        result.Habits.BestStreak.ShouldBe(0);
        result.Habits.DailyBreakdown.Count.ShouldBe(7);
        result.Goals.Active.ShouldBe(0);
        result.Goals.Completed.ShouldBe(0);
        result.Goals.MilestonesCompleted.ShouldBe(0);
        result.DailyLogs.DaysLogged.ShouldBe(0);
        result.DailyLogs.WordCount.ShouldBe(0);
        result.Achievements.ShouldBeEmpty();
        result.Reflection.Exists.ShouldBeFalse();
        result.Reflection.Notes.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_AnyDateInWeek_NormalizesToMonday()
    {
        // Act
        var result = await _sut.Handle(new GetWeeklyReviewQuery(Thu), CancellationToken.None);

        // Assert
        result.WeekStart.ShouldBe(Mon);
        result.WeekEnd.ShouldBe(Sun);
    }

    [Fact]
    public async Task Handle_AggregatesFullWeek_FromTransactionsHabitsGoalsLogsAchievements()
    {
        // Arrange ── habits
        var habitA = new Habit { UserId = _userId, Name = "Read", Entries = new List<HabitEntry>() };
        var habitB = new Habit { UserId = _userId, Name = "Run", Entries = new List<HabitEntry>() };
        _db.Habits.AddRange(habitA, habitB);

        AddEntry(habitA, Mon);
        AddEntry(habitA, Tue);
        AddEntry(habitA, Wed);
        AddEntry(habitA, Thu);
        AddEntry(habitB, Mon);
        AddEntry(habitB, Tue);

        // ── XP + level
        var achievement = new Achievement
        {
            Id = Guid.NewGuid(),
            Key = "first_flame",
            Name = "First Flame",
            Description = "test",
            Category = AchievementCategory.Streak,
            Icon = "🔥"
        };
        _db.Achievements.Add(achievement);

        _db.XpTransactions.AddRange(
            Xp(10, XpSource.HabitCompletion, Mon),
            Xp(15, XpSource.DailyLog, Wed));
        _db.UserXps.Add(new UserXp { UserId = _userId, TotalXp = 100, CurrentLevel = 2 });

        // ── goals + milestones
        var goal = new Goal
        {
            UserId = _userId,
            Title = "Learn C#",
            Status = GoalStatus.Active,
            Milestones = new List<Milestone>()
        };
        goal.Milestones.Add(new Milestone { Title = "m1", IsCompleted = true, CompletedAt = Wed.ToDateTime(new TimeOnly(10, 0)) });
        goal.Milestones.Add(new Milestone { Title = "m2", IsCompleted = false });
        _db.Goals.Add(goal);

        var completedGoal = new Goal
        {
            UserId = _userId,
            Title = "Done",
            Status = GoalStatus.Completed,
            CompletedAt = Fri.ToDateTime(new TimeOnly(10, 0)),
            Milestones = new List<Milestone>()
        };
        _db.Goals.Add(completedGoal);

        // ── daily logs
        _db.DailyLogs.AddRange(
            new DailyLog { UserId = _userId, Date = Mon, Content = "Hello world" },
            new DailyLog { UserId = _userId, Date = Wed, Content = "A longer entry here" });

        // ── achievement unlocked this week
        _db.UserAchievements.Add(new UserAchievement
        {
            UserId = _userId,
            AchievementId = achievement.Id,
            Achievement = achievement,
            UnlockedAt = Tue.ToDateTime(new TimeOnly(9, 0))
        });

        // ── reflection
        _db.WeeklyReflections.Add(new WeeklyReflection
        {
            UserId = _userId,
            WeekStart = Mon,
            Notes = "Great week!",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(new GetWeeklyReviewQuery(Mon), CancellationToken.None);

        // Assert ── XP
        result.XpEarned.Total.ShouldBe(25);
        result.XpEarned.Daily.Count.ShouldBe(2);
        result.XpEarned.Daily.Single(d => d.Date == Mon).Amount.ShouldBe(10);
        result.XpEarned.Daily.Single(d => d.Date == Wed).Amount.ShouldBe(15);

        // Level change: 100 total, 25 earned this week → 75 before the week.
        // 75 XP = level 1; 100 XP = level 2.
        result.LevelChange.From.ShouldBe(1);
        result.LevelChange.To.ShouldBe(2);

        // ── habits
        result.Habits.Total.ShouldBe(2);
        result.Habits.Completed.ShouldBe(6);
        result.Habits.Rate.ShouldBe(42.9); // 6 / 14
        result.Habits.BestStreak.ShouldBe(4); // habitA Mon-Thu
        result.Habits.DailyBreakdown.Count.ShouldBe(7);
        result.Habits.DailyBreakdown.Single(d => d.Date == Mon).Rate.ShouldBe(100);
        result.Habits.DailyBreakdown.Single(d => d.Date == Wed).Rate.ShouldBe(50);
        result.Habits.DailyBreakdown.Single(d => d.Date == Sun).Rate.ShouldBe(0);

        // ── goals
        result.Goals.Active.ShouldBe(1);
        result.Goals.Completed.ShouldBe(1);
        result.Goals.MilestonesCompleted.ShouldBe(1);

        // ── daily logs
        result.DailyLogs.DaysLogged.ShouldBe(2);
        result.DailyLogs.WordCount.ShouldBe(6);

        // ── achievements
        result.Achievements.Count.ShouldBe(1);
        result.Achievements[0].Key.ShouldBe("first_flame");
        result.Achievements[0].Icon.ShouldBe("🔥");

        // ── reflection
        result.Reflection.Exists.ShouldBeTrue();
        result.Reflection.Notes.ShouldBe("Great week!");
    }

    [Fact]
    public async Task Handle_BestStreak_UsesLongestRunAcrossHabits()
    {
        // Arrange
        var habitA = new Habit { UserId = _userId, Name = "A", Entries = new List<HabitEntry>() };
        var habitB = new Habit { UserId = _userId, Name = "B", Entries = new List<HabitEntry>() };
        _db.Habits.AddRange(habitA, habitB);

        // A: Mon..Sat = 6-day streak. B: Sun only.
        foreach (var day in new[] { Mon, Tue, Wed, Thu, Fri, Sat })
            AddEntry(habitA, day);
        AddEntry(habitB, Sun);

        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(new GetWeeklyReviewQuery(Mon), CancellationToken.None);

        // Assert
        result.Habits.BestStreak.ShouldBe(6);
    }

    [Fact]
    public async Task Handle_LevelChange_IsZero_WhenNoXpEarnedThisWeek()
    {
        // Arrange
        _db.UserXps.Add(new UserXp { UserId = _userId, TotalXp = 500, CurrentLevel = 3 });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(new GetWeeklyReviewQuery(Mon), CancellationToken.None);

        // Assert
        result.LevelChange.From.ShouldBe(result.LevelChange.To);
    }

    [Fact]
    public async Task Handle_ExcludesArchivedHabits()
    {
        // Arrange
        _db.Habits.Add(new Habit
        {
            UserId = _userId,
            Name = "Archived",
            IsArchived = true,
            Entries = new List<HabitEntry> { CompletedEntry(Mon) }
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(new GetWeeklyReviewQuery(Mon), CancellationToken.None);

        // Assert
        result.Habits.Total.ShouldBe(0);
        result.Habits.Completed.ShouldBe(0);
    }

    private void AddEntry(Habit habit, DateOnly date)
        => habit.Entries.Add(CompletedEntry(date));

    private static HabitEntry CompletedEntry(DateOnly date)
        => new()
        {
            Date = date,
            IsCompleted = true,
            CompletedAt = date.ToDateTime(new TimeOnly(12, 0))
        };

    private XpTransaction Xp(int amount, XpSource source, DateOnly date)
        => new()
        {
            UserId = _userId,
            Amount = amount,
            Source = source,
            ReferenceId = Guid.NewGuid().ToString(),
            CreatedAt = date.ToDateTime(new TimeOnly(12, 0))
        };

    public void Dispose() => _db.Dispose();
}
