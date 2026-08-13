using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.Habits.Commands.ToggleHabitEntry;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Habits;

public class ToggleHabitEntryCommandHandlerTests
{
    private readonly IHabitRepository _repository;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;
    private readonly ToggleHabitEntryCommandHandler _sut;

    public ToggleHabitEntryCommandHandlerTests()
    {
        _repository = Substitute.For<IHabitRepository>();
        _xpAwardService = Substitute.For<IXpAwardService>();
        _achievementChecker = Substitute.For<IAchievementChecker>();

        // By default, awards no XP, no level-up, not idempotent.
        _xpAwardService.AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new XpAwardResult(0, null, null, false, false));

        // By default, no achievements unlock.
        _achievementChecker.CheckAndUnlockAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Achievement>());

        _sut = new ToggleHabitEntryCommandHandler(_repository, _xpAwardService, _achievementChecker);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenHabitNotFound()
    {
        // Arrange
        _repository.GetByIdAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();
        var command = new ToggleHabitEntryCommand(999, DateOnly.FromDateTime(DateTime.UtcNow));

        // Act & Assert
        var ex = await Should.ThrowAsync<KeyNotFoundException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldContain("was not found");
    }

    [Fact]
    public async Task Handle_CreatesNewCompletedEntry_WhenNoneExists()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var habit = new Habit { Id = 1, Name = "Test Habit", UserId = Guid.NewGuid() };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);
        _repository.GetEntryAsync(1, date, Arg.Any<CancellationToken>()).ReturnsNull();
        _repository.GetAllCompletedDatesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<DateOnly> { date }));

        var command = new ToggleHabitEntryCommand(1, date);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        _repository.Received(1).AddEntry(Arg.Is<HabitEntry>(e =>
            e.HabitId == 1 &&
            e.Date == date &&
            e.IsCompleted == true &&
            e.CompletedAt.HasValue));

        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        result.Data.IsCompletedToday.ShouldBeTrue();
        result.Data.CurrentStreak.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_FlipsExistingEntry_WhenEntryExists()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var habit = new Habit { Id = 1, Name = "Test Habit", UserId = Guid.NewGuid() };
        var existingEntry = new HabitEntry { HabitId = 1, Date = date, IsCompleted = true, CompletedAt = DateTime.UtcNow };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);
        _repository.GetEntryAsync(1, date, Arg.Any<CancellationToken>()).Returns(Task.FromResult(existingEntry)!);

        // Return empty list because we just unchecked it
        _repository.GetAllCompletedDatesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<DateOnly>()));

        var command = new ToggleHabitEntryCommand(1, date);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        existingEntry.IsCompleted.ShouldBeFalse(); // Flag was flipped
        existingEntry.CompletedAt.ShouldBeNull();   // CompletedAt was cleared
        _repository.DidNotReceive().AddEntry(Arg.Any<HabitEntry>()); // Did not add a new one
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());

        result.Data.IsCompletedToday.ShouldBeFalse();
        result.Data.CurrentStreak.ShouldBe(0);

        // No XP awarded on un-complete
        await _xpAwardService.DidNotReceive().AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Awards10Xp_OnNewCompletion()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var userId = Guid.NewGuid();
        var habit = new Habit { Id = 1, Name = "Test Habit", UserId = userId };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);
        _repository.GetEntryAsync(1, date, Arg.Any<CancellationToken>()).ReturnsNull();
        _repository.GetAllCompletedDatesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<DateOnly> { date }));

        // Act
        await _sut.Handle(new ToggleHabitEntryCommand(1, date), CancellationToken.None);

        // Assert — base 10 XP for habit completion (reference id includes date)
        var expectedRef = $"{1}_{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}";
        await _xpAwardService.Received(1).AwardXpAsync(
            userId, 10, XpSource.HabitCompletion, expectedRef, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AwardsStreakBonus_WhenStreakIs7()
    {
        // Arrange: 7 consecutive dates ending today → streak is 7
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dates = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-i)).ToList();
        var userId = Guid.NewGuid();
        var habit = new Habit { Id = 1, Name = "Test Habit", UserId = userId };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);
        _repository.GetEntryAsync(1, today, Arg.Any<CancellationToken>()).ReturnsNull();
        _repository.GetAllCompletedDatesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(dates));

        // Act
        await _sut.Handle(new ToggleHabitEntryCommand(1, today), CancellationToken.None);

        // Assert — base 10 + 50 streak bonus (reference id includes date)
        var expectedRef = $"{1}_{DateOnly.FromDateTime(DateTime.UtcNow):yyyy-MM-dd}";
        await _xpAwardService.Received(1).AwardXpAsync(
            userId, 10, XpSource.HabitCompletion, expectedRef, Arg.Any<CancellationToken>());
        await _xpAwardService.Received(1).AwardXpAsync(
            userId, 50, XpSource.StreakMilestone, expectedRef, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotAwardStreakBonus_WhenStreakIs6()
    {
        // Arrange: streak is 6, no bonus expected
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dates = Enumerable.Range(0, 6)
            .Select(i => today.AddDays(-i)).ToList();
        var userId = Guid.NewGuid();
        var habit = new Habit { Id = 1, Name = "Test Habit", UserId = userId };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);
        _repository.GetEntryAsync(1, today, Arg.Any<CancellationToken>()).ReturnsNull();
        _repository.GetAllCompletedDatesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(dates));

        // Act
        await _sut.Handle(new ToggleHabitEntryCommand(1, today), CancellationToken.None);

        // Assert — only base XP, no streak bonus
        await _xpAwardService.Received(1).AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _xpAwardService.DidNotReceive().AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), XpSource.StreakMilestone,
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RunsAchievementChecker_OnCompletion()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var habit = new Habit { Id = 1, Name = "Test Habit", UserId = Guid.NewGuid() };

        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);
        _repository.GetEntryAsync(1, date, Arg.Any<CancellationToken>()).ReturnsNull();
        _repository.GetAllCompletedDatesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<DateOnly> { date }));

        // Act
        await _sut.Handle(new ToggleHabitEntryCommand(1, date), CancellationToken.None);

        // Assert
        await _achievementChecker.Received(1).CheckAndUnlockAsync(Arg.Any<CancellationToken>());
    }
}