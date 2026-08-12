using NSubstitute;
using QuestLog.Application.Achievements.Queries;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using QuestLog.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Achievements;

public class GetAchievementsQueryHandlerTests : IDisposable
{
    private readonly TestDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetAchievementsQueryHandler _sut;

    public GetAchievementsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);
        _currentUserService = Substitute.For<ICurrentUserService>();
        _sut = new GetAchievementsQueryHandler(_db, _currentUserService);

        SeedAchievements();
    }

    [Fact]
    public async Task Handle_ReturnsAllAchievements_WithLockedStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        // Act
        var result = await _sut.Handle(new GetAchievementsQuery(), CancellationToken.None);

        // Assert
        result.Count.ShouldBe(16);
        result.ShouldAllBe(a => !a.Unlocked);
        result.ShouldAllBe(a => a.UnlockedAt == null);
    }

    [Fact]
    public async Task Handle_ReturnsUnlockedStatus_WhenAchievementUnlocked()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        var firstFlame = _db.Achievements.First(a => a.Key == "first_flame");
        _db.UserAchievements.Add(new UserAchievement
        {
            UserId = userId,
            AchievementId = firstFlame.Id,
            UnlockedAt = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc)
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(new GetAchievementsQuery(), CancellationToken.None);

        // Assert
        result.Count.ShouldBe(16);
        var unlocked = result.First(a => a.Key == "first_flame");
        unlocked.Unlocked.ShouldBeTrue();
        unlocked.UnlockedAt.ShouldBe(new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc));

        var locked = result.Where(a => a.Key != "first_flame").ToList();
        locked.ShouldAllBe(a => !a.Unlocked);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectCategories()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        // Act
        var result = await _sut.Handle(new GetAchievementsQuery(), CancellationToken.None);

        // Assert
        result.Count(a => a.Category == AchievementCategory.Streak).ShouldBe(4);
        result.Count(a => a.Category == AchievementCategory.Milestone).ShouldBe(4);
        result.Count(a => a.Category == AchievementCategory.Consistency).ShouldBe(4);
        result.Count(a => a.Category == AchievementCategory.Special).ShouldBe(4);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectKeys()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        // Act
        var result = await _sut.Handle(new GetAchievementsQuery(), CancellationToken.None);

        // Assert
        var keys = result.Select(a => a.Key).OrderBy(k => k).ToList();
        keys.ShouldBe(new[]
        {
            "century_club", "committed", "dedicated", "early_bird",
            "first_flame", "goal_getter", "journal_keeper", "level_legend",
            "milestone_marker", "monthly_master", "night_owl", "overachiever",
            "perfect_week", "rising_star", "unstoppable", "week_warrior"
        });
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
