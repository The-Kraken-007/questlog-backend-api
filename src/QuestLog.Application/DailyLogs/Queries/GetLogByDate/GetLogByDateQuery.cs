using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.DailyLogs.DTOs;

namespace QuestLog.Application.DailyLogs.Queries.GetLogByDate;

public record GetLogByDateQuery(DateOnly Date) : IRequest<DailyLogDto?>;

public class GetLogByDateQueryHandler : IRequestHandler<GetLogByDateQuery, DailyLogDto?>
{
    private readonly IAppDbContext _db;

    public GetLogByDateQueryHandler(IAppDbContext db) => _db = db;

    public async Task<DailyLogDto?> Handle(GetLogByDateQuery request, CancellationToken cancellationToken)
    {
        var log = await _db.DailyLogs
            .FirstOrDefaultAsync(l => l.Date == request.Date, cancellationToken);

        if (log is null) return null;

        return new DailyLogDto
        {
            Id        = log.Id,
            Date      = log.Date,
            Content   = log.Content,
            CreatedAt = log.CreatedAt,
            UpdatedAt = log.UpdatedAt
        };
    }
}
