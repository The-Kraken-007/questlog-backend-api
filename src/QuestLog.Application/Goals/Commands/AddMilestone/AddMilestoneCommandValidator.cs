using FluentValidation;

namespace QuestLog.Application.Goals.Commands.AddMilestone;

public class AddMilestoneCommandValidator : AbstractValidator<AddMilestoneCommand>
{
    public AddMilestoneCommandValidator()
    {
        RuleFor(x => x.GoalId)
            .GreaterThan(0).WithMessage("A valid goal ID is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Milestone title is required.")
            .MaximumLength(200).WithMessage("Milestone title must not exceed 200 characters.");
    }
}
