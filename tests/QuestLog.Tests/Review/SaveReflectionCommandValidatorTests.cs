using FluentValidation.TestHelper;
using QuestLog.Application.Review.Commands;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Review;

public class SaveReflectionCommandValidatorTests
{
    private readonly SaveReflectionCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
    {
        var result = _validator.TestValidate(
            new SaveReflectionCommand(new DateOnly(2026, 7, 20), "Great week"));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_EmptyNotes_Fails()
    {
        var result = _validator.TestValidate(
            new SaveReflectionCommand(new DateOnly(2026, 7, 20), ""));

        result.IsValid.ShouldBeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_WhitespaceNotes_Fails()
    {
        var result = _validator.TestValidate(
            new SaveReflectionCommand(new DateOnly(2026, 7, 20), "   "));

        result.IsValid.ShouldBeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validate_DefaultWeekStart_Fails()
    {
        var result = _validator.TestValidate(
            new SaveReflectionCommand(default, "notes"));

        result.IsValid.ShouldBeFalse();
        result.ShouldHaveValidationErrorFor(x => x.WeekStart);
    }
}
