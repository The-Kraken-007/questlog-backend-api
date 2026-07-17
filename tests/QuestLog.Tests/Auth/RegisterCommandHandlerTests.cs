using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuestLog.Application.Auth.Commands.Register;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Tests.Common;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Auth;

public class RegisterCommandHandlerTests
{
    private readonly TestDbContext _db;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TestDbContext(options);

        _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
        _passwordHasher = Substitute.For<IPasswordHasher>();

        _sut = new RegisterCommandHandler(_db, _jwtTokenGenerator, _passwordHasher);
    }

    [Fact]
    public async Task Handle_ThrowsException_WhenEmailAlreadyExists()
    {
        // Arrange
        _db.Users.Add(new User { Username = "old", Email = "test@test.com", PasswordHash = "hash" });
        await _db.SaveChangesAsync();

        var command = new RegisterCommand("new", "test@test.com", "pass");

        // Act & Assert
        var ex = await Should.ThrowAsync<InvalidOperationException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldBe("An account with this email already exists.");
    }

    [Fact]
    public async Task Handle_ReturnsToken_WhenUserIsRegistered()
    {
        // Arrange
        var command = new RegisterCommand("testuser", "test@test.com", "password123");
        
        _passwordHasher.Hash("password123").Returns("hashedPassword");
        _jwtTokenGenerator.GenerateToken(Arg.Any<Guid>(), "test@test.com", "testuser").Returns("fake-jwt-token");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Token.ShouldBe("fake-jwt-token");
        result.Username.ShouldBe("testuser");
        result.Email.ShouldBe("test@test.com");

        var dbUser = await _db.Users.SingleAsync();
        dbUser.Username.ShouldBe("testuser");
        dbUser.Email.ShouldBe("test@test.com");
        dbUser.PasswordHash.ShouldBe("hashedPassword");
    }
}
