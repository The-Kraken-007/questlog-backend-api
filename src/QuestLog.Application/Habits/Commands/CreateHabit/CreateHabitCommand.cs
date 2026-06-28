using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.DTOs;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Habits.Commands.CreateHabit;

// ── Command ──────────────────────────────────────────────────────────────────

/// <summary>Request to create a new habit.</summary>
public record CreateHabitCommand(
    string Name,
    string Emoji
) : IRequest<HabitDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class CreateHabitCommandHandler : IRequestHandler<CreateHabitCommand, HabitDto>
{
    private readonly IAppDbContext _db;

    public CreateHabitCommandHandler(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<HabitDto> Handle(CreateHabitCommand request, CancellationToken cancellationToken)
    {
        // Determine next sort order (append to end)
        int maxOrder = await _db.Habits
            .Where(h => !h.IsArchived)
            .Select(h => (int?)h.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var habit = new Habit
        {
            Name      = request.Name.Trim(),
            Emoji     = string.IsNullOrWhiteSpace(request.Emoji) ? "✅" : request.Emoji.Trim(),
            SortOrder = maxOrder + 1,
            CreatedAt = DateTime.UtcNow
        };

        _db.Habits.Add(habit);
        await _db.SaveChangesAsync(cancellationToken);

        return new HabitDto
        {
            Id               = habit.Id,
            Name             = habit.Name,
            Emoji            = habit.Emoji,
            IsArchived       = habit.IsArchived,
            SortOrder        = habit.SortOrder,
            CreatedAt        = habit.CreatedAt,
            IsCompletedToday = false,
            CurrentStreak    = 0
        };
    }
}
