using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.Commands.UpdateHabit;
using QuestLog.Domain.Entities;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Habits;

public class UpdateHabitCommandHandlerTests
{
    private readonly IHabitRepository _repository;
    private readonly UpdateHabitCommandHandler _sut;

    public UpdateHabitCommandHandlerTests()
    {
        _repository = Substitute.For<IHabitRepository>();
        _sut = new UpdateHabitCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenHabitNotFound()
    {
        // Arrange
        _repository.GetByIdWithEntriesAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();
        var command = new UpdateHabitCommand(999, "New Name", null, null, null);

        // Act & Assert
        var ex = await Should.ThrowAsync<KeyNotFoundException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldContain("was not found");
    }

    [Fact]
    public async Task Handle_AppliesPartialUpdates_OnlyForProvidedFields()
    {
        // Arrange
        var habit = new Habit
        {
            Id = 1,
            Name = "Old Name",
            Emoji = "❌",
            SortOrder = 5,
            IsArchived = false
        };
        
        _repository.GetByIdWithEntriesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(habit)!);

        var command = new UpdateHabitCommand(1, "  New Name  ", null, 10, true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        habit.Name.ShouldBe("New Name"); // Trimmed
        habit.Emoji.ShouldBe("❌"); // Unchanged because it was null in command
        habit.SortOrder.ShouldBe(10); // Updated
        habit.IsArchived.ShouldBeTrue(); // Updated

        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
