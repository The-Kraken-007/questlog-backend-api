using NSubstitute;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.Commands.CreateHabit;
using QuestLog.Domain.Entities;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Habits;

public class CreateHabitCommandHandlerTests
{
    private readonly IHabitRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly CreateHabitCommandHandler _sut;

    public CreateHabitCommandHandlerTests()
    {
        _repository = Substitute.For<IHabitRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        
        // Mock current user ID
        _currentUserService.UserId.Returns(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        _sut = new CreateHabitCommandHandler(_repository, _currentUserService);
    }

    [Fact]
    public async Task Handle_SetsCorrectUserIdAndDefaultEmoji()
    {
        // Arrange
        _repository.GetMaxSortOrderAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(5));

        var command = new CreateHabitCommand("Drink Water", "");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Name.ShouldBe("Drink Water");
        result.Emoji.ShouldBe("✅"); // Default emoji
        result.SortOrder.ShouldBe(6); // 5 + 1
        
        _repository.Received(1).Add(Arg.Is<Habit>(h => 
            h.Name == "Drink Water" && 
            h.Emoji == "✅" && 
            h.SortOrder == 6 &&
            h.UserId == Guid.Parse("11111111-1111-1111-1111-111111111111")));
            
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesProvidedEmoji_AndTrimsName()
    {
        // Arrange
        _repository.GetMaxSortOrderAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(0));

        var command = new CreateHabitCommand("  Read Book  ", " 📖 ");

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Name.ShouldBe("Read Book");
        result.Emoji.ShouldBe("📖");
        result.SortOrder.ShouldBe(1);
    }
}
