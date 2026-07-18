using NSubstitute;
using NSubstitute.ReturnsExtensions;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Commands.AddMilestone;
using QuestLog.Domain.Entities;
using Shouldly;
using Xunit;

namespace QuestLog.Tests.Goals;

public class AddMilestoneCommandHandlerTests
{
    private readonly IGoalRepository _repository;
    private readonly AddMilestoneCommandHandler _sut;

    public AddMilestoneCommandHandlerTests()
    {
        _repository = Substitute.For<IGoalRepository>();
        _sut = new AddMilestoneCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_ThrowsKeyNotFoundException_WhenGoalNotFound()
    {
        _repository.GetByIdWithMilestonesAsync(999, Arg.Any<CancellationToken>()).ReturnsNull();
        var command = new AddMilestoneCommand(999, "Test");

        var ex = await Should.ThrowAsync<KeyNotFoundException>(() => _sut.Handle(command, CancellationToken.None));
        ex.Message.ShouldContain("was not found");
    }

    [Fact]
    public async Task Handle_AddsMilestoneWithSortOrder1_WhenGoalHasNoMilestones()
    {
        var goal = new Goal { Id = 1, Milestones = new List<Milestone>() };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>()).Returns(Task.FromResult(goal)!);

        var command = new AddMilestoneCommand(1, "First Milestone");

        // Stub the re-fetch after save: return the goal with the new milestone included.
        var newMilestone = new Milestone { Title = "First Milestone", SortOrder = 1, GoalId = 1 };
        var updatedGoal = new Goal { Id = 1, Milestones = new List<Milestone> { newMilestone } };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(goal)!, Task.FromResult(updatedGoal)!);

        var result = await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).AddMilestone(Arg.Is<Milestone>(m => m.SortOrder == 1 && m.Title == "First Milestone"));
        result.Milestones.Single().SortOrder.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_AddsMilestoneWithNextSortOrder_WhenGoalHasMilestones()
    {
        var existingMilestone = new Milestone { SortOrder = 5 };
        var goal = new Goal 
        { 
            Id = 1, 
            Milestones = new List<Milestone> { existingMilestone } 
        };

        // Stub the re-fetch after save: return the goal with both milestones.
        var newMilestone = new Milestone { SortOrder = 6, GoalId = 1 };
        var updatedGoal = new Goal
        {
            Id = 1,
            Milestones = new List<Milestone> { existingMilestone, newMilestone }
        };
        _repository.GetByIdWithMilestonesAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(goal)!, Task.FromResult(updatedGoal)!);

        var command = new AddMilestoneCommand(1, "Next Milestone");
        var result = await _sut.Handle(command, CancellationToken.None);

        _repository.Received(1).AddMilestone(Arg.Is<Milestone>(m => m.SortOrder == 6));
        result.Milestones.Last().SortOrder.ShouldBe(6);
    }
}
