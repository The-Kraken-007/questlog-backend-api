using QuestLog.Application.Habits.Commands.ToggleHabitEntry;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Habits;

public class ToggleHabitEntryCommandValidatorTests
{
    private readonly ToggleHabitEntryCommandValidator _sut = new();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Validate_Fails_WhenHabitIdIsZero()
    {
        var result = _sut.Validate(new ToggleHabitEntryCommand(HabitId: 0, Date: Today));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "HabitId" && e.ErrorMessage == "A valid habit ID is required.");
    }

    [Fact]
    public void Validate_Fails_WhenHabitIdIsNegative()
    {
        var result = _sut.Validate(new ToggleHabitEntryCommand(HabitId: -5, Date: Today));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "HabitId");
    }

    [Fact]
    public void Validate_Fails_WhenDateIsInTheFuture()
    {
        var tomorrow = Today.AddDays(1);
        var result = _sut.Validate(new ToggleHabitEntryCommand(HabitId: 1, Date: tomorrow));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Date" && e.ErrorMessage == "Cannot toggle a habit entry for a future date.");
    }

    [Fact]
    public void Validate_Passes_WithTodaysDate()
    {
        var result = _sut.Validate(new ToggleHabitEntryCommand(HabitId: 1, Date: Today));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Passes_WithAPastDate()
    {
        var lastWeek = Today.AddDays(-7);
        var result = _sut.Validate(new ToggleHabitEntryCommand(HabitId: 1, Date: lastWeek));

        result.IsValid.ShouldBeTrue();
    }
}
