using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.Commands.ToggleHabitEntry;
using QuestLog.Domain.Entities;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Habits;

public class ToggleHabitEntryCommandHandlerTests
{
    private readonly IHabitRepository _repository;
    private readonly ToggleHabitEntryCommandHandler _sut;

    public ToggleHabitEntryCommandHandlerTests()
    {
        _repository = Substitute.For<IHabitRepository>();
        _sut = new ToggleHabitEntryCommandHandler(_repository);
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
        var habit = new Habit { Id = 1, Name = "Test Habit" };
        
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
            e.IsCompleted == true));
            
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        
        result.IsCompletedToday.ShouldBeTrue();
        result.CurrentStreak.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_FlipsExistingEntry_WhenEntryExists()
    {
        // Arrange
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var habit = new Habit { Id = 1, Name = "Test Habit" };
        var existingEntry = new HabitEntry { HabitId = 1, Date = date, IsCompleted = true };
        
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);
        _repository.GetEntryAsync(1, date, Arg.Any<CancellationToken>()).Returns(Task.FromResult(existingEntry)!);
        
        // Return empty list because we just unchecked it
        _repository.GetAllCompletedDatesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(new List<DateOnly>()));

        var command = new ToggleHabitEntryCommand(1, date);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        existingEntry.IsCompleted.ShouldBeFalse(); // Flag was flipped
        _repository.DidNotReceive().AddEntry(Arg.Any<HabitEntry>()); // Did not add a new one
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        
        result.IsCompletedToday.ShouldBeFalse();
        result.CurrentStreak.ShouldBe(0);
    }
}
