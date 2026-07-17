using FluentValidation;
using MediatR;
using QuestLog.Application.Auth.Commands.Register;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Application.Auth.Commands.Login;

// ── Command ──────────────────────────────────────────────────────────────────

public record LoginCommand(
    string Email,
    string Password
) : IRequest<AuthResponse>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(
        IAppDbContext db,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = _db.Users.SingleOrDefault(u => u.Email == request.Email.ToLower());

        // Use a generic message to avoid leaking whether the email is registered
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        string token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Username);
        return await Task.FromResult(new AuthResponse(token, user.Username, user.Email));
    }
}
