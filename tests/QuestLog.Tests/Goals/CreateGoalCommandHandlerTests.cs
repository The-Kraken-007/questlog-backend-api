using NSubstitute;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Commands.CreateGoal;
using QuestLog.Domain.Entities;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Goals;

public class CreateGoalCommandHandlerTests
{
    private readonly IGoalRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly CreateGoalCommandHandler _sut;

    public CreateGoalCommandHandlerTests()
    {
        _repository = Substitute.For<IGoalRepository>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _currentUserService.UserId.Returns(Guid.Parse("22222222-2222-2222-2222-222222222222"));

        _sut = new CreateGoalCommandHandler(_repository, _currentUserService);
    }

    [Fact]
    public async Task Handle_SetsCorrectUserIdAndTrimsFields()
    {
        var command = new CreateGoalCommand("  My Goal  ", "  Desc  ", new DateOnly(2025, 1, 1));
        var result = await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).Add(Arg.Is<Goal>(g => 
            g.UserId == Guid.Parse("22222222-2222-2222-2222-222222222222") &&
            g.Title == "My Goal" &&
            g.Description == "Desc" &&
            g.TargetDate == new DateOnly(2025, 1, 1)));

        result.Title.ShouldBe("My Goal");
        result.Description.ShouldBe("Desc");
    }
}
