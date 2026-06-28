using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.DailyLogs.DTOs;

namespace QuestLog.Application.DailyLogs.Queries.GetLogsInRange;

public record GetLogsInRangeQuery(DateOnly From, DateOnly To) : IRequest<List<DailyLogDto>>;

public class GetLogsInRangeQueryHandler : IRequestHandler<GetLogsInRangeQuery, List<DailyLogDto>>
{
    private readonly IAppDbContext _db;

    public GetLogsInRangeQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<DailyLogDto>> Handle(GetLogsInRangeQuery request, CancellationToken cancellationToken)
    {
        var logs = await _db.DailyLogs
            .Where(l => l.Date >= request.From && l.Date <= request.To)
            .OrderBy(l => l.Date)
            .ToListAsync(cancellationToken);

        return logs.Select(l => new DailyLogDto
        {
            Id        = l.Id,
            Date      = l.Date,
            Content   = l.Content,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt
        }).ToList();
    }
}
