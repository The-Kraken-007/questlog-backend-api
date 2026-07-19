using QuestLog.Application.QuestTasks.Commands;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class CreateQuestTaskCommandValidatorTests
{
    private readonly CreateQuestTaskCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenNameIsEmpty()
    {
        var result = _sut.Validate(new CreateQuestTaskCommand(1, "", null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_Fails_WhenNameExceeds200Characters()
    {
        var result = _sut.Validate(new CreateQuestTaskCommand(1, new string('a', 201), null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_Fails_WhenQuestTaskListIdIsZeroOrNegative()
    {
        var resultZero = _sut.Validate(new CreateQuestTaskCommand(0, "Task", null));
        var resultNeg = _sut.Validate(new CreateQuestTaskCommand(-1, "Task", null));

        resultZero.IsValid.ShouldBeFalse();
        resultNeg.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Passes_WithValidCommandAndNullDueDate()
    {
        var result = _sut.Validate(new CreateQuestTaskCommand(1, "Buy groceries", null));

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Validate_Passes_WithValidCommandAndDueDate()
    {
        var result = _sut.Validate(new CreateQuestTaskCommand(1, "Buy groceries", DateTime.UtcNow.AddDays(1)));

        result.IsValid.ShouldBeTrue();
    }
}
