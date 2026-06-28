using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Common;
using QuestLog.Application.Goals.DTOs;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.Goals.Commands.UpdateGoal;

/// <summary>
/// Partial update — only non-null fields are applied.
/// Passing Status = null leaves the current status unchanged.
/// </summary>
public record UpdateGoalCommand(
    int Id,
    string? Title,
    string? Description,
    DateOnly? TargetDate,
    GoalStatus? Status
) : IRequest<GoalDto>;

public class UpdateGoalCommandHandler : IRequestHandler<UpdateGoalCommand, GoalDto>
{
    private readonly IAppDbContext _db;

    public UpdateGoalCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GoalDto> Handle(UpdateGoalCommand request, CancellationToken cancellationToken)
    {
        var goal = await _db.Goals
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goal with ID {request.Id} was not found.");

        if (request.Title is not null)
            goal.Title = request.Title.Trim();

        if (request.Description is not null)
            goal.Description = request.Description.Trim();

        if (request.TargetDate.HasValue)
            goal.TargetDate = request.TargetDate;

        if (request.Status.HasValue)
        {
            goal.Status = request.Status.Value;
            // Stamp CompletedAt when manually marking as completed
            if (request.Status.Value == GoalStatus.Completed && goal.CompletedAt is null)
                goal.CompletedAt = DateTime.UtcNow;
            else if (request.Status.Value != GoalStatus.Completed)
                goal.CompletedAt = null;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return GoalMapper.ToDto(goal);
    }
}
