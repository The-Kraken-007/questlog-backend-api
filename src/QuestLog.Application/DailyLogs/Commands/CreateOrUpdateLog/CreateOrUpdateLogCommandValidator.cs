using FluentValidation;

namespace QuestLog.Application.DailyLogs.Commands.CreateOrUpdateLog;

public class CreateOrUpdateLogCommandValidator : AbstractValidator<CreateOrUpdateLogCommand>
{
    public CreateOrUpdateLogCommandValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("A valid date is required.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Log content cannot be empty.");
    }
}
