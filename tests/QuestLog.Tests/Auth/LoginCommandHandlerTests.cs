using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuestLog.Application.Auth.Commands.Login;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Auth;

public class LoginCommandHandlerTests
{
    private readonly TestDbContext _db;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);

        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _passwordHasher = Substitute.For<IPasswordHasher>();

        _sut = new LoginCommandHandler(_db, _jwtTokenGenerator, _passwordHasher);
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenUserNotFound()
    {
        // Arrange
        var command = new LoginCommand("wrong@test.com", "pass");

        // Act & Assert
        var ex = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_ThrowsUnauthorized_WhenPasswordIsIncorrect()
    {
        // Arrange
        _db.Users.Add(new User { Id = Guid.NewGuid(), Username = "test", Email = "test@test.com", PasswordHash = "hash" });
        await _db.SaveChangesAsync();

        _passwordHasher.Verify("wrongpass", "hash").Returns(false);

        var command = new LoginCommand("test@test.com", "wrongpass");

        // Act & Assert
        var ex = await Should.ThrowAsync<UnauthorizedAccessException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Handle_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _db.Users.Add(new User { Id = userId, Username = "test", Email = "test@test.com", PasswordHash = "hash" });
        await _db.SaveChangesAsync();

        _passwordHasher.Verify("correctpass", "hash").Returns(true);
        _jwtTokenGenerator.GenerateToken(userId, "test@test.com", "test").Returns("fake-jwt");

        var command = new LoginCommand("test@test.com", "correctpass");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Token.ShouldBe("fake-jwt");
        result.Username.ShouldBe("test");
        result.Email.ShouldBe("test@test.com");
    }
}
