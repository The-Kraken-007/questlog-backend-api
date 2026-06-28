using FluentValidation;

namespace QuestLog.Application.Habits.Commands.UpdateHabit;

public class UpdateHabitCommandValidator : AbstractValidator<UpdateHabitCommand>
{
    public UpdateHabitCommandValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("A valid habit ID is required.");

        // Name and Emoji are optional (partial update), but if provided must be valid
        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty().WithMessage("Habit name cannot be empty if provided.")
                .MaximumLength(100).WithMessage("Habit name must not exceed 100 characters.");
        });

        When(x => x.Emoji is not null, () =>
        {
            RuleFor(x => x.Emoji!)
                .MaximumLength(10).WithMessage("Emoji must not exceed 10 characters.");
        });

        When(x => x.SortOrder.HasValue, () =>
        {
            RuleFor(x => x.SortOrder!.Value)
                .GreaterThanOrEqualTo(0).WithMessage("Sort order must be a non-negative number.");
        });
    }
}
