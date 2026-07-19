using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.QuestTasks.DTOs;

namespace QuestLog.Application.QuestTasks.Queries;

public record GetQuestTaskListsQuery : IRequest<List<QuestTaskListDto>>;

public class GetQuestTaskListsQueryHandler : IRequestHandler<GetQuestTaskListsQuery, List<QuestTaskListDto>>
{
    private readonly IAppDbContext _context;

    public GetQuestTaskListsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<QuestTaskListDto>> Handle(GetQuestTaskListsQuery request, CancellationToken cancellationToken)
    {
        var lists = await _context.QuestTaskLists
            .Include(l => l.Tasks)
            .OrderBy(l => l.SortOrder)
            .ToListAsync(cancellationToken);

        return lists.Select(QuestTaskListDto.FromEntity).ToList();
    }
}
