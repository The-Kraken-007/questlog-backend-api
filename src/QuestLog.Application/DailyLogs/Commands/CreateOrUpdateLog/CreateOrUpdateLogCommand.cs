using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.DailyLogs.DTOs;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.DailyLogs.Commands.CreateOrUpdateLog;

public record CreateOrUpdateLogCommand(
    DateOnly Date,
    string Content
) : IRequest<DailyLogDto>;

public class CreateOrUpdateLogCommandHandler : IRequestHandler<CreateOrUpdateLogCommand, DailyLogDto>
{
    private readonly IDailyLogRepository _repository;

    public CreateOrUpdateLogCommandHandler(IDailyLogRepository repository) => _repository = repository;

    public async Task<DailyLogDto> Handle(CreateOrUpdateLogCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByDateAsync(request.Date, cancellationToken);

        if (existing is null)
        {
            // First log for this date — create
            existing = new DailyLog
            {
                Date      = request.Date,
                Content   = request.Content.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _repository.Add(existing);
        }
        else
        {
            // Already exists — update content and bump UpdatedAt
            existing.Content   = request.Content.Trim();
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return ToDto(existing);
    }

    private static DailyLogDto ToDto(DailyLog log) => new()
    {
        Id        = log.Id,
        Date      = log.Date,
        Content   = log.Content,
        CreatedAt = log.CreatedAt,
        UpdatedAt = log.UpdatedAt
    };
}
