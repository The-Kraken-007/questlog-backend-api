using FluentValidation;
using MediatR;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Application.Auth.Commands.ResetPassword;

// ── Command ──────────────────────────────────────────────────────────────────

public record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword
) : IRequest<Unit>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(IAppDbContext db, IPasswordHasher passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = _db.Users.SingleOrDefault(u =>
            u.Email == request.Email.ToLower()
            && u.PasswordResetToken == request.Token
            && u.PasswordResetTokenExpiresAt > DateTime.UtcNow);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid or expired reset token.");

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiresAt = null;

        await _db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
