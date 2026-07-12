using QuestLog.Application.Habits.Commands.CreateHabit;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Habits;

public class CreateHabitCommandValidatorTests
{
    private readonly CreateHabitCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenNameIsEmpty()
    {
        var result = _sut.Validate(new CreateHabitCommand(Name: "", Emoji: "✅"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Habit name is required.");
    }

    [Fact]
    public void Validate_Fails_WhenNameExceeds100Characters()
    {
        var longName = new string('x', 101);
        var result = _sut.Validate(new CreateHabitCommand(Name: longName, Emoji: "✅"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name" && e.ErrorMessage == "Habit name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_Fails_WhenEmojiExceeds10Characters()
    {
        var longEmoji = new string('a', 11);
        var result = _sut.Validate(new CreateHabitCommand(Name: "Morning Run", Emoji: longEmoji));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Emoji" && e.ErrorMessage == "Emoji must not exceed 10 characters.");
    }

    [Fact]
    public void Validate_Passes_WithValidCommand()
    {
        var result = _sut.Validate(new CreateHabitCommand(Name: "Morning Run", Emoji: "🏃"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Passes_WhenNameIsExactly100Characters()
    {
        var maxName = new string('x', 100);
        var result = _sut.Validate(new CreateHabitCommand(Name: maxName, Emoji: "✅"));

        result.IsValid.ShouldBeTrue();
    }
}
