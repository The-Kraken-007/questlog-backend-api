using NSubstitute;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using QuestLog.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Achievements;

public class AchievementCheckerTests : IDisposable
{
    private readonly TestDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly AchievementChecker _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public AchievementCheckerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(_userId);
        _sut = new AchievementChecker(_db, _currentUserService);

        SeedAchievements();
    }

    [Fact]
    public async Task CheckAndUnlock_FirstFlame_WhenHabitCompleted()
    {
        // Arrange — add a completed habit entry
        var habit = new Habit { UserId = _userId, Name = "Test" };
        _db.Habits.Add(habit);
        _db.HabitEntries.Add(new HabitEntry { Habit = habit, Date = DateOnly.FromDateTime(DateTime.UtcNow), IsCompleted = true });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "first_flame");
    }

    [Fact]
    public async Task CheckAndUnlock_GoalGetter_WhenGoalCompleted()
    {
        // Arrange
        _db.Goals.Add(new Goal { UserId = _userId, Title = "Done", Status = GoalStatus.Completed });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "goal_getter");
    }

    [Fact]
    public async Task CheckAndUnlock_Overachiever_When10GoalsCompleted()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
            _db.Goals.Add(new Goal { UserId = _userId, Title = $"Goal {i}", Status = GoalStatus.Completed });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "overachiever");
    }

    [Fact]
    public async Task CheckAndUnlock_MilestoneMarker_When5MilestonesCompleted()
    {
        // Arrange
        var goal = new Goal { UserId = _userId, Title = "Big Goal" };
        _db.Goals.Add(goal);
        for (int i = 0; i < 5; i++)
            _db.Milestones.Add(new Milestone { Goal = goal, Title = $"M{i}", IsCompleted = true });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "milestone_marker");
    }

    [Fact]
    public async Task CheckAndUnlock_Unstoppable_When100Completions()
    {
        // Arrange
        var habit = new Habit { UserId = _userId, Name = "Test" };
        _db.Habits.Add(habit);
        for (int i = 0; i < 100; i++)
            _db.HabitEntries.Add(new HabitEntry { Habit = habit, Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)), IsCompleted = true });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "unstoppable");
    }

    [Fact]
    public async Task CheckAndUnlock_JournalKeeper_When30Logs()
    {
        // Arrange
        for (int i = 0; i < 30; i++)
            _db.DailyLogs.Add(new DailyLog { UserId = _userId, Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)), Content = $"Entry {i}" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "journal_keeper");
    }

    [Fact]
    public async Task CheckAndUnlock_RisingStar_WhenLevel10()
    {
        // Arrange — level 10 requires 11102 XP
        _db.UserXps.Add(new UserXp { UserId = _userId, TotalXp = 11102, CurrentLevel = 10 });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "rising_star");
    }

    [Fact]
    public async Task CheckAndUnlock_NightOwl_WhenCompletedAfterMidnight()
    {
        // Arrange
        var habit = new Habit { UserId = _userId, Name = "Test" };
        _db.Habits.Add(habit);
        _db.HabitEntries.Add(new HabitEntry
        {
            Habit = habit,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow.Date.AddHours(2) // 2 AM UTC
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "night_owl");
    }

    [Fact]
    public async Task CheckAndUnlock_EarlyBird_WhenCompletedBefore7AM()
    {
        // Arrange
        var habit = new Habit { UserId = _userId, Name = "Test" };
        _db.Habits.Add(habit);
        _db.HabitEntries.Add(new HabitEntry
        {
            Habit = habit,
            Date = DateOnly.FromDateTime(DateTime.UtcNow),
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow.Date.AddHours(6) // 6 AM UTC
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "early_bird");
    }

    [Fact]
    public async Task CheckAndUnlock_DoesNotReunlock_AlreadyUnlocked()
    {
        // Arrange — unlock first_flame
        var habit = new Habit { UserId = _userId, Name = "Test" };
        _db.Habits.Add(habit);
        _db.HabitEntries.Add(new HabitEntry { Habit = habit, Date = DateOnly.FromDateTime(DateTime.UtcNow), IsCompleted = true });
        await _db.SaveChangesAsync();

        await _sut.CheckAndUnlockAsync(); // First call unlocks

        // Act — second call should not re-unlock
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldNotContain(a => a.Key == "first_flame");
    }

    [Fact]
    public async Task CheckAndUnlock_ReturnsEmpty_WhenNoConditionsMet()
    {
        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task CheckAndUnlock_WeekWarrior_When7DayStreak()
    {
        // Arrange — create 7 consecutive days of completions ending today
        var habit = new Habit { UserId = _userId, Name = "Test" };
        _db.Habits.Add(habit);
        for (int i = 0; i < 7; i++)
        {
            _db.HabitEntries.Add(new HabitEntry
            {
                Habit = habit,
                Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                IsCompleted = true
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert
        result.ShouldContain(a => a.Key == "week_warrior");
    }

    [Fact]
    public async Task CheckAndUnlock_DoesNotUnlockOtherUsersAchievements()
    {
        // Arrange — another user's data
        var otherUserId = Guid.NewGuid();
        var habit = new Habit { UserId = otherUserId, Name = "Other" };
        _db.Habits.Add(habit);
        _db.HabitEntries.Add(new HabitEntry { Habit = habit, Date = DateOnly.FromDateTime(DateTime.UtcNow), IsCompleted = true });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.CheckAndUnlockAsync();

        // Assert — our user should not get first_flame from another user's habit
        result.ShouldNotContain(a => a.Key == "first_flame");
    }

    private void SeedAchievements()
    {
        var achievements = new[]
        {
            new Achievement { Id = Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001"), Key = "first_flame", Name = "First Flame", Description = "Complete any habit for the first time", Category = AchievementCategory.Streak, Icon = "🔥" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0002-4000-8000-000000000002"), Key = "week_warrior", Name = "Week Warrior", Description = "Maintain a 7-day streak on any habit", Category = AchievementCategory.Streak, Icon = "⚔️" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0003-4000-8000-000000000003"), Key = "monthly_master", Name = "Monthly Master", Description = "Maintain a 30-day streak on any habit", Category = AchievementCategory.Streak, Icon = "🛡️" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0004-4000-8000-000000000004"), Key = "century_club", Name = "Century Club", Description = "Maintain a 100-day streak on any habit", Category = AchievementCategory.Streak, Icon = "👑" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005"), Key = "goal_getter", Name = "Goal Getter", Description = "Complete your first goal", Category = AchievementCategory.Milestone, Icon = "🎯" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0006-4000-8000-000000000006"), Key = "overachiever", Name = "Overachiever", Description = "Complete 10 goals", Category = AchievementCategory.Milestone, Icon = "🏆" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0007-4000-8000-000000000007"), Key = "milestone_marker", Name = "Milestone Marker", Description = "Complete 5 goal milestones", Category = AchievementCategory.Milestone, Icon = "📌" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0008-4000-8000-000000000008"), Key = "perfect_week", Name = "Perfect Week", Description = "Complete at least one habit every day for 7 consecutive days", Category = AchievementCategory.Milestone, Icon = "⭐" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0009-4000-8000-000000000009"), Key = "dedicated", Name = "Dedicated", Description = "Complete habits 5 days in a week", Category = AchievementCategory.Consistency, Icon = "📅" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0010-4000-8000-000000000010"), Key = "committed", Name = "Committed", Description = "Complete habits 20 days in a month", Category = AchievementCategory.Consistency, Icon = "🗓️" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0011-4000-8000-000000000011"), Key = "unstoppable", Name = "Unstoppable", Description = "Complete habits 100 total times", Category = AchievementCategory.Consistency, Icon = "💪" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0012-4000-8000-000000000012"), Key = "rising_star", Name = "Rising Star", Description = "Reach Level 10", Category = AchievementCategory.Consistency, Icon = "🌟" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0013-4000-8000-000000000013"), Key = "night_owl", Name = "Night Owl", Description = "Complete a habit after midnight (UTC)", Category = AchievementCategory.Special, Icon = "🦉" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0014-4000-8000-000000000014"), Key = "early_bird", Name = "Early Bird", Description = "Complete a habit before 7 AM (UTC)", Category = AchievementCategory.Special, Icon = "🐦" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0015-4000-8000-000000000015"), Key = "journal_keeper", Name = "Journal Keeper", Description = "Write 30 daily log entries", Category = AchievementCategory.Special, Icon = "📝" },
            new Achievement { Id = Guid.Parse("a1b2c3d4-0016-4000-8000-000000000016"), Key = "level_legend", Name = "Level Legend", Description = "Reach Level 50", Category = AchievementCategory.Special, Icon = "🎖️" }
        };

        _db.Achievements.AddRange(achievements);
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
