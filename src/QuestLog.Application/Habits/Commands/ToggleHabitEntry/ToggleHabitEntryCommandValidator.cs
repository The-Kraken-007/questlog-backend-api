using FluentValidation;

namespace QuestLog.Application.Habits.Commands.ToggleHabitEntry;

public class ToggleHabitEntryCommandValidator : AbstractValidator<ToggleHabitEntryCommand>
{
    public ToggleHabitEntryCommandValidator()
    {
        RuleFor(x => x.HabitId)
            .GreaterThan(0).WithMessage("A valid habit ID is required.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("A valid date is required.")
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Cannot toggle a habit entry for a future date.");
    }
}
