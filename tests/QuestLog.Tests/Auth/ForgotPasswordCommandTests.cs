using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Auth.Commands.ForgotPassword;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Auth;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenEmailIsInvalid()
    {
        var result = _sut.Validate(new ForgotPasswordCommand("not-an-email"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == "Email" && e.ErrorMessage.Contains("valid email address"));
    }

    [Fact]
    public void Validate_Passes_WithValidEmail()
    {
        var result = _sut.Validate(new ForgotPasswordCommand("test@test.com"));

        result.IsValid.ShouldBeTrue();
    }
}

public class ForgotPasswordCommandHandlerTests
{
    private readonly TestDbContext _db;
    private readonly ForgotPasswordCommandHandler _sut;

    public ForgotPasswordCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);

        _sut = new ForgotPasswordCommandHandler(_db);
    }

    [Fact]
    public async Task Handle_ReturnsUnit_AndDoesNothing_WhenUserNotFound()
    {
        // Arrange
        var command = new ForgotPasswordCommand("missing@test.com");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
    }

    [Fact]
    public async Task Handle_GeneratesResetToken_WhenUserExists()
    {
        // Arrange
        _db.Users.Add(new User { Username = "test", Email = "test@test.com", PasswordHash = "hash" });
        await _db.SaveChangesAsync();

        var command = new ForgotPasswordCommand("test@test.com");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBe(Unit.Value);
        
        var dbUser = await _db.Users.SingleAsync();
        dbUser.PasswordResetToken.ShouldNotBeNullOrEmpty();
        dbUser.PasswordResetTokenExpiresAt.ShouldNotBeNull();
        dbUser.PasswordResetTokenExpiresAt.Value.ShouldBeGreaterThan(DateTime.UtcNow);
    }
}
