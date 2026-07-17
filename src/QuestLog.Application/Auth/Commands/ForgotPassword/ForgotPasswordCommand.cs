using FluentValidation;
using MediatR;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Application.Auth.Commands.ForgotPassword;

// ── Command ──────────────────────────────────────────────────────────────────

public record ForgotPasswordCommand(string Email) : IRequest<Unit>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Unit>
{
    private readonly IAppDbContext _db;

    public ForgotPasswordCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = _db.Users.SingleOrDefault(u => u.Email == request.Email.ToLower());

        // Always return success — do NOT reveal whether the email exists (security)
        if (user is null)
            return Unit.Value;

        // Generate a secure URL-safe random token valid for 1 hour
        string resetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                                   .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);

        await _db.SaveChangesAsync(cancellationToken);

        // TODO: Replace with a real email service (e.g. SendGrid) when available.
        // For now, print the token to the console so it can be copied during development.
        Console.WriteLine($"[DEV - MOCK EMAIL] Password reset token for {user.Email}: {resetToken}");

        return Unit.Value;
    }
}
