using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Common;
using QuestLog.Application.Goals.DTOs;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Goals.Commands.AddMilestone;

public record AddMilestoneCommand(
    int GoalId,
    string Title
) : IRequest<GoalDto>;

public class AddMilestoneCommandHandler : IRequestHandler<AddMilestoneCommand, GoalDto>
{
    private readonly IAppDbContext _db;

    public AddMilestoneCommandHandler(IAppDbContext db) => _db = db;

    public async Task<GoalDto> Handle(AddMilestoneCommand request, CancellationToken cancellationToken)
    {
        var goal = await _db.Goals
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == request.GoalId, cancellationToken)
            ?? throw new KeyNotFoundException($"Goal with ID {request.GoalId} was not found.");

        int nextOrder = goal.Milestones.Count > 0
            ? goal.Milestones.Max(m => m.SortOrder) + 1
            : 1;

        var milestone = new Milestone
        {
            GoalId    = request.GoalId,
            Title     = request.Title.Trim(),
            SortOrder = nextOrder
        };

        _db.Milestones.Add(milestone);
        await _db.SaveChangesAsync(cancellationToken);

        // Reload to include the new milestone in the returned DTO
        goal.Milestones.Add(milestone);

        return GoalMapper.ToDto(goal);
    }
}
