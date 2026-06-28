using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Common;
using QuestLog.Application.Goals.DTOs;

namespace QuestLog.Application.Goals.Queries.GetGoalById;

public record GetGoalByIdQuery(int Id) : IRequest<GoalDto>;

public class GetGoalByIdQueryHandler : IRequestHandler<GetGoalByIdQuery, GoalDto>
{
    private readonly IAppDbContext _db;

    public GetGoalByIdQueryHandler(IAppDbContext db) => _db = db;

    public async Task<GoalDto> Handle(GetGoalByIdQuery request, CancellationToken cancellationToken)
    {
        var goal = await _db.Goals
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Goal with ID {request.Id} was not found.");

        return GoalMapper.ToDto(goal);
    }
}
