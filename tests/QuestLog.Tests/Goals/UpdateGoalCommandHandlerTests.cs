using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.Goals.Commands.UpdateGoal;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Goals;

public class UpdateGoalCommandHandlerTests
{
    private readonly IGoalRepository _repository;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;
    private readonly UpdateGoalCommandHandler _sut;

    public UpdateGoalCommandHandlerTests()
    {
        _repository = Substitute.For<IGoalRepository>();
        _xpAwardService = Substitute.For<IXpAwardService>();
        _achievementChecker = Substitute.For<IAchievementChecker>();

        _xpAwardService.AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new XpAwardResult(0, null, null, false, false));

        _achievementChecker.CheckAndUnlockAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Achievement>());

        _sut = new UpdateGoalCommandHandler(_repository, _xpAwardService, _achievementChecker);
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
        var goal = new Goal { Id = 1, UserId = Guid.NewGuid(), Status = GoalStatus.Active, CompletedAt = null };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new UpdateGoalCommand(1, null, null, null, GoalStatus.Completed), CancellationToken.None);

        goal.Status.ShouldBe(GoalStatus.Completed);
        goal.CompletedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_ClearsCompletedAt_WhenStatusChangedFromCompleted()
    {
        var goal = new Goal { Id = 1, UserId = Guid.NewGuid(), Status = GoalStatus.Completed, CompletedAt = DateTime.UtcNow };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new UpdateGoalCommand(1, null, null, null, GoalStatus.Paused), CancellationToken.None);

        goal.Status.ShouldBe(GoalStatus.Paused);
        goal.CompletedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_Awards100Xp_WhenTransitionedToCompleted()
    {
        var userId = Guid.NewGuid();
        var goal = new Goal { Id = 1, UserId = userId, Status = GoalStatus.Active, CompletedAt = null };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        await _sut.Handle(new UpdateGoalCommand(1, null, null, null, GoalStatus.Completed), CancellationToken.None);

        await _xpAwardService.Received(1).AwardXpAsync(
            userId, 100, XpSource.GoalCompletion, "1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotAwardXp_WhenStatusUnchanged()
    {
        var goal = new Goal { Id = 1, UserId = Guid.NewGuid(), Status = GoalStatus.Active };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        // No Status field passed → no XP award attempt
        await _sut.Handle(new UpdateGoalCommand(1, "New Title", null, null, null), CancellationToken.None);

        await _xpAwardService.DidNotReceive().AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotAwardXp_WhenAlreadyCompleted()
    {
        var goal = new Goal { Id = 1, UserId = Guid.NewGuid(), Status = GoalStatus.Completed, CompletedAt = DateTime.UtcNow };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        // Status already Completed, passing Completed again → no XP
        await _sut.Handle(new UpdateGoalCommand(1, null, null, null, GoalStatus.Completed), CancellationToken.None);

        await _xpAwardService.DidNotReceive().AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RunsAchievementChecker_WhenCompleted()
    {
        var goal = new Goal { Id = 1, UserId = Guid.NewGuid(), Status = GoalStatus.Active };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        await _sut.Handle(new UpdateGoalCommand(1, null, null, null, GoalStatus.Completed), CancellationToken.None);

        await _achievementChecker.Received(1).CheckAndUnlockAsync(Arg.Any<CancellationToken>());
    }
}