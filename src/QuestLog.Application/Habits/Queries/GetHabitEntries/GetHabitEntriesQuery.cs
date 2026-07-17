using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Habits.DTOs;

namespace QuestLog.Application.Habits.Queries.GetHabitEntries;

// ── Query ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all habit entries for a given habit within an optional date range.
/// Designed to feed the calendar heatmap on the frontend.
/// </summary>
public record GetHabitEntriesQuery(
    int HabitId,
    DateOnly? From = null,
    DateOnly? To = null
) : IRequest<List<HabitEntryDto>>;

// ── Handler ───────────────────────────────────────────────────────────────────

public class GetHabitEntriesQueryHandler : IRequestHandler<GetHabitEntriesQuery, List<HabitEntryDto>>
{
    private readonly IHabitRepository _repository;

    public GetHabitEntriesQueryHandler(IHabitRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<HabitEntryDto>> Handle(GetHabitEntriesQuery request, CancellationToken cancellationToken)
    {
        // Verify the habit exists
        bool habitExists = await _repository.ExistsAsync(request.HabitId, cancellationToken);
        if (!habitExists)
            throw new KeyNotFoundException($"Habit with ID {request.HabitId} was not found.");

        var entries = await _repository.GetEntriesAsync(request.HabitId, request.From, request.To, cancellationToken);

        return entries.Select(e => new HabitEntryDto
        {
            Id          = e.Id,
            HabitId     = e.HabitId,
            Date        = e.Date,
            IsCompleted = e.IsCompleted
        }).ToList();
    }
}
