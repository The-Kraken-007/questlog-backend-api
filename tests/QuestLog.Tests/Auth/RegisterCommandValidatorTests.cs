using QuestLog.Application.Auth.Commands.Register;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenEmailIsInvalid()
    {
        var result = _sut.Validate(new RegisterCommand("testuser", "not-an-email", "password123"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("valid email address"));
    }

    [Fact]
    public void Validate_Fails_WhenPasswordIsTooShort()
    {
        var result = _sut.Validate(new RegisterCommand("testuser", "test@test.com", "Ab1!"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("at least 8 characters"));
    }

    [Fact]
    public void Validate_Fails_WhenPasswordHasNoUppercase()
    {
        var result = _sut.Validate(new RegisterCommand("testuser", "test@test.com", "password1!"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("uppercase letter"));
    }

    [Fact]
    public void Validate_Fails_WhenPasswordHasNoSpecialCharacter()
    {
        var result = _sut.Validate(new RegisterCommand("testuser", "test@test.com", "Password1"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Password" && e.ErrorMessage.Contains("special character"));
    }

    [Fact]
    public void Validate_Passes_WithValidCommand()
    {
        var result = _sut.Validate(new RegisterCommand("testuser", "test@test.com", "Password1!"));

        result.IsValid.ShouldBeTrue();
    }
}
