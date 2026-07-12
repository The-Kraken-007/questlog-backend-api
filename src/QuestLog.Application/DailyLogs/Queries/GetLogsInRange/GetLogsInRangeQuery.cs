using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.DailyLogs.DTOs;

namespace QuestLog.Application.DailyLogs.Queries.GetLogsInRange;

public record GetLogsInRangeQuery(DateOnly From, DateOnly To) : IRequest<List<DailyLogDto>>;

public class GetLogsInRangeQueryHandler : IRequestHandler<GetLogsInRangeQuery, List<DailyLogDto>>
{
    private readonly IDailyLogRepository _repository;

    public GetLogsInRangeQueryHandler(IDailyLogRepository repository) => _repository = repository;

    public async Task<List<DailyLogDto>> Handle(GetLogsInRangeQuery request, CancellationToken cancellationToken)
    {
        var logs = await _repository.GetLogsInRangeAsync(request.From, request.To, cancellationToken);

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
