using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Common;
using QuestLog.Application.Goals.DTOs;

namespace QuestLog.Application.Goals.Queries.GetAllGoals;

public record GetAllGoalsQuery : IRequest<List<GoalDto>>;

public class GetAllGoalsQueryHandler : IRequestHandler<GetAllGoalsQuery, List<GoalDto>>
{
    private readonly IAppDbContext _db;

    public GetAllGoalsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<GoalDto>> Handle(GetAllGoalsQuery request, CancellationToken cancellationToken)
    {
        var goals = await _db.Goals
            .Include(g => g.Milestones)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        return goals.Select(GoalMapper.ToDto).ToList();
    }
}
