using FluentValidation;
using MediatR;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Auth.Commands.Register;

// ── DTO ───────────────────────────────────────────────────────────────────────

public record AuthResponse(string Token, string Username, string Email);

// ── Command ──────────────────────────────────────────────────────────────────

public record RegisterCommand(
    string Username,
    string Email,
    string Password
) : IRequest<AuthResponse>;

// ── Validator ─────────────────────────────────────────────────────────────────

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

// ── Handler ───────────────────────────────────────────────────────────────────

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(
        IAppDbContext db,
        IJwtTokenGenerator jwtTokenGenerator,
        IPasswordHasher passwordHasher)
    {
        _db = db;
        _jwtTokenGenerator = jwtTokenGenerator;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Check if email is already taken
        bool emailExists = _db.Users.Any(u => u.Email == request.Email.ToLower());
        if (emailExists)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Username     = request.Username.Trim(),
            Email        = request.Email.Trim().ToLower(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            CreatedAt    = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        string token = _jwtTokenGenerator.GenerateToken(user.Id, user.Email, user.Username);
        return new AuthResponse(token, user.Username, user.Email);
    }
}
