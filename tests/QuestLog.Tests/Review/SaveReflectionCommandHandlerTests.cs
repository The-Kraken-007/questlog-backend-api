using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Review.Commands;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Review;

public class SaveReflectionCommandHandlerTests : IDisposable
{
    private readonly TestDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly SaveReflectionCommandHandler _sut;
    private readonly Guid _userId;

    public SaveReflectionCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);
        _userId = Guid.NewGuid();
        _currentUser = Substitute.For<ICurrentUserService>();
        _currentUser.UserId.Returns(_userId);
        _sut = new SaveReflectionCommandHandler(_db, _currentUser);
    }

    [Fact]
    public async Task Handle_CreatesNewReflection_WhenNoneExists()
    {
        // Act
        var result = await _sut.Handle(
            new SaveReflectionCommand(new DateOnly(2026, 7, 20), "Felt great!"),
            CancellationToken.None);

        // Assert
        result.Exists.ShouldBeTrue();
        result.Notes.ShouldBe("Felt great!");

        var saved = await _db.WeeklyReflections.SingleAsync();
        saved.UserId.ShouldBe(_userId);
        saved.WeekStart.ShouldBe(new DateOnly(2026, 7, 20));
        saved.Notes.ShouldBe("Felt great!");
    }

    [Fact]
    public async Task Handle_UpdatesExistingReflection_InsteadOfCreatingDuplicate()
    {
        // Arrange
        _db.WeeklyReflections.Add(new WeeklyReflection
        {
            UserId = _userId,
            WeekStart = new DateOnly(2026, 7, 20),
            Notes = "Original"
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.Handle(
            new SaveReflectionCommand(new DateOnly(2026, 7, 20), "Updated notes"),
            CancellationToken.None);

        // Assert
        result.Notes.ShouldBe("Updated notes");
        (await _db.WeeklyReflections.CountAsync()).ShouldBe(1);

        var saved = await _db.WeeklyReflections.SingleAsync();
        saved.Notes.ShouldBe("Updated notes");
    }

    [Fact]
    public async Task Handle_NormalizesAnyDayToMonday()
    {
        // Act — Thursday of the week
        var result = await _sut.Handle(
            new SaveReflectionCommand(new DateOnly(2026, 7, 23), "notes"),
            CancellationToken.None);

        // Assert
        result.Exists.ShouldBeTrue();
        var saved = await _db.WeeklyReflections.SingleAsync();
        saved.WeekStart.ShouldBe(new DateOnly(2026, 7, 20));
    }

    [Fact]
    public async Task Handle_Unauthenticated_Throws()
    {
        // Arrange
        _currentUser.UserId.Returns((Guid?)null);

        // Act / Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _sut.Handle(new SaveReflectionCommand(new DateOnly(2026, 7, 20), "notes"), CancellationToken.None));
    }

    public void Dispose() => _db.Dispose();
}
