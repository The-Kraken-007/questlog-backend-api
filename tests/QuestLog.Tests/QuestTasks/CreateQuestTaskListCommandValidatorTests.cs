using QuestLog.Application.QuestTasks.Commands;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.QuestTasks;

public class CreateQuestTaskListCommandValidatorTests
{
    private readonly CreateQuestTaskListCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenNameIsEmpty()
    {
        var result = _sut.Validate(new CreateQuestTaskListCommand(""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_Fails_WhenNameIsNullOrWhitespace(string? name)
    {
        var result = _sut.Validate(new CreateQuestTaskListCommand(name!));

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenNameExceeds100Characters()
    {
        var result = _sut.Validate(new CreateQuestTaskListCommand(new string('a', 101)));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_Passes_WithValidName()
    {
        var result = _sut.Validate(new CreateQuestTaskListCommand("Shopping"));

        result.IsValid.ShouldBeTrue();
    }
}
