using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.DailyLogs.DTOs;

namespace QuestLog.Application.DailyLogs.Queries.GetLogByDate;

public record GetLogByDateQuery(DateOnly Date) : IRequest<DailyLogDto?>;

public class GetLogByDateQueryHandler : IRequestHandler<GetLogByDateQuery, DailyLogDto?>
{
    private readonly IDailyLogRepository _repository;

    public GetLogByDateQueryHandler(IDailyLogRepository repository) => _repository = repository;

    public async Task<DailyLogDto?> Handle(GetLogByDateQuery request, CancellationToken cancellationToken)
    {
        var log = await _repository.GetByDateAsync(request.Date, cancellationToken);

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
