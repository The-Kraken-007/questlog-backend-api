using QuestLog.Application.Auth.Commands.Login;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenEmailIsInvalid()
    {
        var result = _sut.Validate(new LoginCommand("not-an-email", "password123"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("valid email address"));
    }

    [Fact]
    public void Validate_Fails_WhenPasswordIsEmpty()
    {
        var result = _sut.Validate(new LoginCommand("test@test.com", ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("must not be empty"));
    }

    [Fact]
    public void Validate_Passes_WithValidCommand()
    {
        var result = _sut.Validate(new LoginCommand("test@test.com", "password123"));

        result.IsValid.ShouldBeTrue();
    }
}
