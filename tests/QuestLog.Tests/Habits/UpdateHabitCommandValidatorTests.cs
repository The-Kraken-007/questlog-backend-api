using QuestLog.Application.Habits.Commands.UpdateHabit;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Habits;

public class UpdateHabitCommandValidatorTests
{
    private readonly UpdateHabitCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenIdIsZero()
    {
        var result = _sut.Validate(new UpdateHabitCommand(Id: 0, Name: null, Emoji: null, SortOrder: null, IsArchived: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Id" && e.ErrorMessage == "A valid habit ID is required.");
    }

    [Fact]
    public void Validate_Fails_WhenIdIsNegative()
    {
        var result = _sut.Validate(new UpdateHabitCommand(Id: -1, Name: null, Emoji: null, SortOrder: null, IsArchived: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_Fails_WhenNameIsProvided_ButEmpty()
    {
        // Name != null triggers the conditional rule; empty string should fail
        var result = _sut.Validate(new UpdateHabitCommand(Id: 1, Name: "", Emoji: null, SortOrder: null, IsArchived: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Habit name cannot be empty if provided.");
    }

    [Fact]
    public void Validate_Fails_WhenNameExceeds100Characters()
    {
        var longName = new string('x', 101);
        var result = _sut.Validate(new UpdateHabitCommand(Id: 1, Name: longName, Emoji: null, SortOrder: null, IsArchived: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Habit name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_Fails_WhenEmojiExceeds10Characters()
    {
        var longEmoji = new string('a', 11);
        var result = _sut.Validate(new UpdateHabitCommand(Id: 1, Name: null, Emoji: longEmoji, SortOrder: null, IsArchived: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Emoji" && e.ErrorMessage == "Emoji must not exceed 10 characters.");
    }

    [Fact]
    public void Validate_Fails_WhenSortOrderIsNegative()
    {
        var result = _sut.Validate(new UpdateHabitCommand(Id: 1, Name: null, Emoji: null, SortOrder: -1, IsArchived: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "SortOrder.Value" && e.ErrorMessage == "Sort order must be a non-negative number.");
    }

    [Fact]
    public void Validate_Passes_WithOnlyIdProvided()
    {
        // All optional fields as null — minimum valid partial update
        var result = _sut.Validate(new UpdateHabitCommand(Id: 1, Name: null, Emoji: null, SortOrder: null, IsArchived: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Passes_WithAllFieldsProvided()
    {
        var result = _sut.Validate(new UpdateHabitCommand(Id: 1, Name: "Evening Walk", Emoji: "🚶", SortOrder: 2, IsArchived: false));

        result.IsValid.ShouldBeTrue();
    }
}
