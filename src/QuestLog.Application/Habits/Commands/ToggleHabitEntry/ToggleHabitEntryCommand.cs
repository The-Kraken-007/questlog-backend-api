using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.Common;
using QuestLog.Application.Habits.DTOs;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Habits.Commands.ToggleHabitEntry;

// ── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Toggles the completion status of a habit for a given date.
/// If no entry exists for that date, one is created (IsCompleted = true).
/// If an entry already exists, its IsCompleted flag is flipped.
/// </summary>
public record ToggleHabitEntryCommand(
    int HabitId,
    DateOnly Date
) : IRequest<HabitDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class ToggleHabitEntryCommandHandler : IRequestHandler<ToggleHabitEntryCommand, HabitDto>
{
    private readonly IHabitRepository _repository;

    public ToggleHabitEntryCommandHandler(IHabitRepository repository)
    {
        _repository = repository;
    }

    public async Task<HabitDto> Handle(ToggleHabitEntryCommand request, CancellationToken cancellationToken)
    {
        // Ensure the habit exists
        var habit = await _repository.GetByIdAsync(request.HabitId, cancellationToken)
            ?? throw new KeyNotFoundException($"Habit with ID {request.HabitId} was not found.");

        // Upsert: find existing entry for this date or create a new one
        var entry = await _repository.GetEntryAsync(request.HabitId, request.Date, cancellationToken);

        if (entry is null)
        {
            // No entry yet → create it as completed
            entry = new HabitEntry
            {
                HabitId     = request.HabitId,
                Date        = request.Date,
                IsCompleted = true,
                CreatedAt   = DateTime.UtcNow
            };
            _repository.AddEntry(entry);
        }
        else
        {
            // Entry exists → flip the flag
            entry.IsCompleted = !entry.IsCompleted;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        // Reload all completed dates to calculate streak
        var allCompletedDates = await _repository.GetAllCompletedDatesAsync(request.HabitId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool isCompletedToday = allCompletedDates.Contains(today);
        int streak = StreakCalculator.Calculate(allCompletedDates, today);

        return new HabitDto
        {
            Id               = habit.Id,
            Name             = habit.Name,
            Emoji            = habit.Emoji,
            IsArchived       = habit.IsArchived,
            SortOrder        = habit.SortOrder,
            CreatedAt        = habit.CreatedAt,
            IsCompletedToday = isCompletedToday,
            CurrentStreak    = streak
        };
    }
}
