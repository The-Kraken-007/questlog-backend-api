using NSubstitute;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Gamification.Queries;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Gamification;

public class GetGamificationProfileQueryHandlerTests : IDisposable
{
    private readonly TestDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetGamificationProfileQueryHandler _sut;

    public GetGamificationProfileQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);
        _currentUserService = Substitute.For<ICurrentUserService>();
        _sut = new GetGamificationProfileQueryHandler(_db, _currentUserService);
    }

    [Fact]
    public async Task Handle_ReturnsZeroProfile_WhenNoUserXpExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        // Act
        var result = await _sut.Handle(new GetGamificationProfileQuery(), CancellationToken.None);

        // Assert
        result.TotalXp.ShouldBe(0);
        result.CurrentLevel.ShouldBe(1);
        result.Title.ShouldBe("Novice");
        result.XpInCurrentLevel.ShouldBe(0);
        result.XpForCurrentLevel.ShouldBe(100);
        result.XpToNextLevel.ShouldBe(100);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectProfile_WhenUserXpExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        _db.UserXps.Add(new UserXp
        {
            UserId = userId,
            TotalXp = 150,
            CurrentLevel = 2,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(new GetGamificationProfileQuery(), CancellationToken.None);

        // Assert
        result.TotalXp.ShouldBe(150);
        result.CurrentLevel.ShouldBe(2);
        result.Title.ShouldBe("Novice");
        result.XpInCurrentLevel.ShouldBe(50);
        result.XpForCurrentLevel.ShouldBe(282);
        result.XpToNextLevel.ShouldBe(232);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectProfile_ForHighLevelUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserService.UserId.Returns(userId);

        _db.UserXps.Add(new UserXp
        {
            UserId = userId,
            TotalXp = 11102,
            CurrentLevel = 10,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(new GetGamificationProfileQuery(), CancellationToken.None);

        // Assert
        result.TotalXp.ShouldBe(11102);
        result.CurrentLevel.ShouldBe(10);
        result.Title.ShouldBe("Adept");
        result.XpInCurrentLevel.ShouldBe(0);
        result.XpForCurrentLevel.ShouldBe(3162);
        result.XpToNextLevel.ShouldBe(3162);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
