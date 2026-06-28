using FluentValidation;

namespace QuestLog.Application.Goals.Commands.UpdateGoal;

public class UpdateGoalCommandValidator : AbstractValidator<UpdateGoalCommand>
{
    public UpdateGoalCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("A valid goal ID is required.");

        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title!)
                .NotEmpty().WithMessage("Goal title cannot be empty if provided.")
                .MaximumLength(200).WithMessage("Goal title must not exceed 200 characters.");
        });
    }
}
