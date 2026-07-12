using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Goals.Common;
using QuestLog.Application.Goals.DTOs;

namespace QuestLog.Application.Goals.Queries.GetAllGoals;

public record GetAllGoalsQuery : IRequest<List<GoalDto>>;

public class GetAllGoalsQueryHandler : IRequestHandler<GetAllGoalsQuery, List<GoalDto>>
{
    private readonly IGoalRepository _repository;

    public GetAllGoalsQueryHandler(IGoalRepository repository) => _repository = repository;

    public async Task<List<GoalDto>> Handle(GetAllGoalsQuery request, CancellationToken cancellationToken)
    {
        var goals = await _repository.GetAllWithMilestonesAsync(cancellationToken);

        return goals.Select(GoalMapper.ToDto).ToList();
    }
}
