using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Review.Common;
using QuestLog.Application.Review.DTOs;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Review.Commands;

// ── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Upserts the user's reflection note for a given week. One note per user per
/// week — saving again overwrites the previous note in place.
/// </summary>
public record SaveReflectionCommand(DateOnly WeekStart, string Notes) : IRequest<ReflectionDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class SaveReflectionCommandHandler : IRequestHandler<SaveReflectionCommand, ReflectionDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SaveReflectionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ReflectionDto> Handle(SaveReflectionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var weekStart = WeekHelper.StartOfWeek(request.WeekStart);

        var reflection = await _db.WeeklyReflections
            .FirstOrDefaultAsync(w => w.WeekStart == weekStart, ct);

        if (reflection is null)
        {
            reflection = new WeeklyReflection
            {
                UserId    = userId,
                WeekStart = weekStart,
                Notes     = request.Notes.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.WeeklyReflections.Add(reflection);
        }
        else
        {
            reflection.Notes     = request.Notes.Trim();
            reflection.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);

        return new ReflectionDto { Notes = reflection.Notes, Exists = true };
    }
}

// ── Validator ─────────────────────────────────────────────────────────────────

public class SaveReflectionCommandValidator : AbstractValidator<SaveReflectionCommand>
{
    public SaveReflectionCommandValidator()
    {
        RuleFor(x => x.WeekStart)
            .NotEmpty().WithMessage("A valid week start date is required.");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Reflection notes cannot be empty.");
    }
}
