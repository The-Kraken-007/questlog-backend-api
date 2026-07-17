using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Commands.UpdateGoal;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Goals;

public class UpdateGoalCommandHandlerTests
{
    private readonly IGoalRepository _repository;
    private readonly UpdateGoalCommandHandler _sut;

    public UpdateGoalCommandHandlerTests()
    {
        _repository = Substitute.For<IGoalRepository>();
        _sut = new UpdateGoalCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFound_WhenGoalNotFound()
    {
        _repository.GetByIdWithMilestonesAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();
        var ex = await Should.ThrowAsync<KeyNotFoundException>(() => _sut.Handle(new UpdateGoalCommand(999, null, null, null, null), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_StampsCompletedAt_WhenStatusChangedToCompleted()
    {
        var goal = new Goal { Id = 1, Status = GoalStatus.Active, CompletedAt = null };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new UpdateGoalCommand(1, null, null, null, GoalStatus.Completed), CancellationToken.None);

        goal.Status.ShouldBe(GoalStatus.Completed);
        goal.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_ClearsCompletedAt_WhenStatusChangedFromCompleted()
    {
        var goal = new Goal { Id = 1, Status = GoalStatus.Completed, CompletedAt = DateTime.UtcNow };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new UpdateGoalCommand(1, null, null, null, GoalStatus.Paused), CancellationToken.None);

        goal.Status.ShouldBe(GoalStatus.Paused);
        goal.CompletedAt.ShouldBeNull();
    }
}
