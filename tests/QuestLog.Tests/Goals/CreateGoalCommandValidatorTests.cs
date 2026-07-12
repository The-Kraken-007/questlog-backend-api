using QuestLog.Application.Goals.Commands.CreateGoal;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Goals;

public class CreateGoalCommandValidatorTests
{
    private readonly CreateGoalCommandValidator _sut = new();

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public void Validate_Fails_WhenTitleIsEmpty()
    {
        var result = _sut.Validate(new CreateGoalCommand(Title: "", Description: null, TargetDate: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Title" && e.ErrorMessage == "Goal title is required.");
    }

    [Fact]
    public void Validate_Fails_WhenTitleExceeds200Characters()
    {
        var longTitle = new string('x', 201);
        var result = _sut.Validate(new CreateGoalCommand(Title: longTitle, Description: null, TargetDate: null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Title" && e.ErrorMessage == "Goal title must not exceed 200 characters.");
    }

    [Fact]
    public void Validate_Fails_WhenTargetDateIsInThePast()
    {
        var yesterday = Today.AddDays(-1);
        var result = _sut.Validate(new CreateGoalCommand(Title: "Learn Piano", Description: null, TargetDate: yesterday));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "TargetDate.Value" && e.ErrorMessage == "Target date must be today or in the future.");
    }

    [Fact]
    public void Validate_Passes_WithTitleOnly()
    {
        var result = _sut.Validate(new CreateGoalCommand(Title: "Learn Piano", Description: null, TargetDate: null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Passes_WithTodayAsTargetDate()
    {
        var result = _sut.Validate(new CreateGoalCommand(Title: "Learn Piano", Description: null, TargetDate: Today));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Passes_WithFutureTargetDate()
    {
        var nextYear = Today.AddYears(1);
        var result = _sut.Validate(new CreateGoalCommand(Title: "Run a marathon", Description: "My first marathon", TargetDate: nextYear));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Passes_WhenTitleIsExactly200Characters()
    {
        var maxTitle = new string('x', 200);
        var result = _sut.Validate(new CreateGoalCommand(Title: maxTitle, Description: null, TargetDate: null));

        result.IsValid.ShouldBeTrue();
    }
}
