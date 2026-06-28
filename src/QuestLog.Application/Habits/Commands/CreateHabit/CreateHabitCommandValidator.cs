using FluentValidation;

namespace QuestLog.Application.Habits.Commands.CreateHabit;

public class CreateHabitCommandValidator : AbstractValidator<CreateHabitCommand>
{
    public CreateHabitCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Habit name is required.")
            .MaximumLength(100).WithMessage("Habit name must not exceed 100 characters.");

        RuleFor(x => x.Emoji)
            .MaximumLength(10).WithMessage("Emoji must not exceed 10 characters.");
    }
}
