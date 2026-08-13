using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.Goals.Commands.ToggleMilestone;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Goals;

public class ToggleMilestoneCommandHandlerTests
{
    private readonly IGoalRepository _repository;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;
    private readonly ToggleMilestoneCommandHandler _sut;

    public ToggleMilestoneCommandHandlerTests()
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

        _sut = new ToggleMilestoneCommandHandler(_repository, _xpAwardService, _achievementChecker);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFound_WhenMilestoneNotFound()
    {
        _repository.GetMilestoneByIdAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();
        var command = new ToggleMilestoneCommand(999);
        var ex = await Should.ThrowAsync<KeyNotFoundException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldContain("was not found");
    }

    [Fact]
    public async Task Handle_AutoCompleteGoal_WhenAllMilestonesAreDone()
    {
        var userId = Guid.NewGuid();
        var milestone = new Milestone { Id = 1, GoalId = 10, IsCompleted = false };
        _repository.GetMilestoneByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(milestone)!);

        var goal = new Goal
        {
            Id = 10,
            UserId = userId,
            Status = GoalStatus.Active,
            Milestones = new List<Milestone> { milestone } // Now it will be IsCompleted = true
        };
        _repository.GetByIdWithMilestonesAsync(10, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new ToggleMilestoneCommand(1), CancellationToken.None);

        milestone.IsCompleted.ShouldBeTrue();
        goal.Status.ShouldBe(GoalStatus.Completed);
        goal.CompletedAt.ShouldNotBeNull();
        result.Data.Status.ShouldBe(GoalStatus.Completed);

        // Awarded milestone (25) + goal auto-completion (100) XP
        await _xpAwardService.Received(1).AwardXpAsync(
            userId, 25, XpSource.GoalMilestone, "1", Arg.Any<CancellationToken>());
        await _xpAwardService.Received(1).AwardXpAsync(
            userId, 100, XpSource.GoalCompletion, "10", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AutoRevertGoal_WhenMilestoneIsUnTicked()
    {
        var milestone = new Milestone { Id = 1, GoalId = 10, IsCompleted = true };
        _repository.GetMilestoneByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(milestone)!);

        var goal = new Goal
        {
            Id = 10,
            UserId = Guid.NewGuid(),
            Status = GoalStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            Milestones = new List<Milestone> { milestone } // Will become IsCompleted = false
        };
        _repository.GetByIdWithMilestonesAsync(10, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new ToggleMilestoneCommand(1), CancellationToken.None);

        milestone.IsCompleted.ShouldBeFalse();
        goal.Status.ShouldBe(GoalStatus.Active);
        goal.CompletedAt.ShouldBeNull();
        result.Data.Status.ShouldBe(GoalStatus.Active);

        // Un-ticking awards no XP
        await _xpAwardService.DidNotReceive().AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<XpSource>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AwardsOnlyMilestoneXp_WhenGoalNotAutoCompleted()
    {
        // 3 milestones: one done, one being ticked, one still undone → goal not done
        var userId = Guid.NewGuid();
        var milestone1 = new Milestone { Id = 1, GoalId = 10, IsCompleted = true };
        var milestone2 = new Milestone { Id = 2, GoalId = 10, IsCompleted = false };
        var milestone3 = new Milestone { Id = 3, GoalId = 10, IsCompleted = false };

        // Toggle milestone2 (will become completed)
        _repository.GetMilestoneByIdAsync(2, Arg.Any<CancellationToken>()).Returns(Task.FromResult(milestone2)!);

        var goal = new Goal
        {
            Id = 10,
            UserId = userId,
            Status = GoalStatus.Active,
            Milestones = new List<Milestone> { milestone1, milestone2, milestone3 }
        };
        _repository.GetByIdWithMilestonesAsync(10, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new ToggleMilestoneCommand(2), CancellationToken.None);

        // Only milestone XP awarded, no goal completion XP
        await _xpAwardService.Received(1).AwardXpAsync(
            userId, 25, XpSource.GoalMilestone, "2", Arg.Any<CancellationToken>());
        await _xpAwardService.DidNotReceive().AwardXpAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), XpSource.GoalCompletion,
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}