using FluentValidation;

namespace QuestLog.Application.Goals.Commands.CreateGoal;

public class CreateGoalCommandValidator : AbstractValidator<CreateGoalCommand>
{
    public CreateGoalCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Goal title is required.")
            .MaximumLength(200).WithMessage("Goal title must not exceed 200 characters.");

        When(x => x.TargetDate.HasValue, () =>
        {
            RuleFor(x => x.TargetDate!.Value)
                .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Target date must be today or in the future.");
        });
    }
}
