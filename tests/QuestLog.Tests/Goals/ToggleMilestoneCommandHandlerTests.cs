using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Commands.ToggleMilestone;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Goals;

public class ToggleMilestoneCommandHandlerTests
{
    private readonly IGoalRepository _repository;
    private readonly ToggleMilestoneCommandHandler _sut;

    public ToggleMilestoneCommandHandlerTests()
    {
        _repository = Substitute.For<IGoalRepository>();
        _sut = new ToggleMilestoneCommandHandler(_repository);
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
        var milestone = new Milestone { Id = 1, GoalId = 10, IsCompleted = false };
        _repository.GetMilestoneByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(milestone)!);

        var goal = new Goal 
        { 
            Id = 10, 
            Status = GoalStatus.Active,
            Milestones = new List<Milestone> { milestone } // Now it will be IsCompleted = true
        };
        _repository.GetByIdWithMilestonesAsync(10, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new ToggleMilestoneCommand(1), CancellationToken.None);

        milestone.IsCompleted.ShouldBeTrue();
        goal.Status.ShouldBe(GoalStatus.Completed);
        goal.CompletedAt.ShouldNotBeNull();
        result.Status.ShouldBe(GoalStatus.Completed);
    }

    [Fact]
    public async Task Handle_AutoRevertGoal_WhenMilestoneIsUnTicked()
    {
        var milestone = new Milestone { Id = 1, GoalId = 10, IsCompleted = true };
        _repository.GetMilestoneByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(milestone)!);

        var goal = new Goal 
        { 
            Id = 10, 
            Status = GoalStatus.Completed,
            CompletedAt = DateTime.UtcNow,
            Milestones = new List<Milestone> { milestone } // Will become IsCompleted = false
        };
        _repository.GetByIdWithMilestonesAsync(10, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var result = await _sut.Handle(new ToggleMilestoneCommand(1), CancellationToken.None);

        milestone.IsCompleted.ShouldBeFalse();
        goal.Status.ShouldBe(GoalStatus.Active);
        goal.CompletedAt.ShouldBeNull();
        result.Status.ShouldBe(GoalStatus.Active);
    }
}
