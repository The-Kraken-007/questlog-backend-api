using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.DTOs;

namespace QuestLog.Application.Habits.Commands.UpdateHabit;

// ── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Request to update an existing habit's name, emoji, sort order, or archived status.
/// Only non-null fields are updated (partial update pattern).
/// </summary>
public record UpdateHabitCommand(
    int Id,
    string? Name,
    string? Emoji,
    int? SortOrder,
    bool? IsArchived
) : IRequest<HabitDto>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class UpdateHabitCommandHandler : IRequestHandler<UpdateHabitCommand, HabitDto>
{
    private readonly IHabitRepository _repository;

    public UpdateHabitCommandHandler(IHabitRepository repository)
    {
        _repository = repository;
    }

    public async Task<HabitDto> Handle(UpdateHabitCommand request, CancellationToken cancellationToken)
    {
        var habit = await _repository.GetByIdWithEntriesAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Habit with ID {request.Id} was not found.");

        // Apply only the fields that were provided
        if (request.Name is not null)
            habit.Name = request.Name.Trim();

        if (request.Emoji is not null)
            habit.Emoji = request.Emoji.Trim();

        if (request.SortOrder.HasValue)
            habit.SortOrder = request.SortOrder.Value;

        if (request.IsArchived.HasValue)
            habit.IsArchived = request.IsArchived.Value;

        await _repository.SaveChangesAsync(cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var completedDates = habit.Entries
            .Where(e => e.IsCompleted)
            .Select(e => e.Date)
            .ToList();

        bool isCompletedToday = completedDates.Contains(today);
        int streak = Common.StreakCalculator.Calculate(completedDates, today);

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
