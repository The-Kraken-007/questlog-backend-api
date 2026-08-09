using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuestLog.Application.Auth.Commands.ResetPassword;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Auth;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _sut = new();

    [Fact]
    public void Validate_Fails_WhenEmailIsInvalid()
    {
        var result = _sut.Validate(new ResetPasswordCommand("not-an-email", "token", "Newpass1!"));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenTokenIsEmpty()
    {
        var result = _sut.Validate(new ResetPasswordCommand("test@test.com", "", "Newpass1!"));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Fails_WhenNewPasswordIsTooShort()
    {
        var result = _sut.Validate(new ResetPasswordCommand("test@test.com", "token", "short"));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Passes_WithValidCommand()
    {
        var result = _sut.Validate(new ResetPasswordCommand("test@test.com", "token", "Newpass1!"));
        result.IsValid.ShouldBeTrue();
    }
}

public class ResetPasswordCommandHandlerTests
{
    private readonly TestDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ResetPasswordCommandHandler _sut;

    public ResetPasswordCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);

        _passwordHasher = Substitute.For<IPasswordHasher>();

        _sut = new ResetPasswordCommandHandler(_db, _passwordHasher);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenUserNotFound()
    {
        var command = new ResetPasswordCommand("wrong@test.com", "token", "Newpass1!");
        var ex = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldBe("Invalid or expired reset token.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTokenIsExpired()
    {
        _db.Users.Add(new User 
        { 
            Username = "test", 
            Email = "test@test.com", 
            PasswordHash = "hash",
            PasswordResetToken = "token",
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1) 
        });
        await _db.SaveChangesAsync();

        var command = new ResetPasswordCommand("test@test.com", "token", "Newpass1!");
        var ex = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldBe("Invalid or expired reset token.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenTokenIsWrong()
    {
        _db.Users.Add(new User 
        { 
            Username = "test", 
            Email = "test@test.com", 
            PasswordHash = "hash",
            PasswordResetToken = "token",
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1) 
        });
        await _db.SaveChangesAsync();

        var command = new ResetPasswordCommand("test@test.com", "wrong-token", "Newpass1!");
        var ex = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldBe("Invalid or expired reset token.");
    }

    [Fact]
    public async Task Handle_ResetsPassword_WhenTokenIsValid()
    {
        _db.Users.Add(new User 
        { 
            Username = "test", 
            Email = "test@test.com", 
            PasswordHash = "hash",
            PasswordResetToken = "token",
            PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1) 
        });
        await _db.SaveChangesAsync();

        _passwordHasher.Hash("Newpass1!").Returns("newHash");

        var command = new ResetPasswordCommand("test@test.com", "token", "Newpass1!");
        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBe(Unit.Value);

        var dbUser = await _db.Users.SingleAsync();
        dbUser.PasswordHash.ShouldBe("newHash");
        dbUser.PasswordResetToken.ShouldBeNull();
        dbUser.PasswordResetTokenExpiresAt.ShouldBeNull();
    }
}
