using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Achievements.Common;
using QuestLog.Application.Achievements.DTOs;
using QuestLog.Application.Common.DTOs;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.DailyLogs.DTOs;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;

namespace QuestLog.Application.DailyLogs.Commands.CreateOrUpdateLog;

/// <summary>
/// Creates a daily log for a date if none exists, otherwise updates the
/// content in place. XP (15) is only awarded on the CREATE branch — editing
/// an existing log does not re-award. Idempotent per (user, date): saving the
/// same date multiple times in a row awards only once.
/// </summary>
public record CreateOrUpdateLogCommand(
    DateOnly Date,
    string Content
) : IRequest<GamifiedResult<DailyLogDto>>;

public class CreateOrUpdateLogCommandHandler : IRequestHandler<CreateOrUpdateLogCommand, GamifiedResult<DailyLogDto>>
{
    private readonly IDailyLogRepository _repository;
    private readonly ICurrentUserService _currentUser;
    private readonly IXpAwardService _xpAwardService;
    private readonly IAchievementChecker _achievementChecker;

    public CreateOrUpdateLogCommandHandler(
        IDailyLogRepository repository,
        ICurrentUserService currentUser,
        IXpAwardService xpAwardService,
        IAchievementChecker achievementChecker)
    {
        _repository = repository;
        _currentUser = currentUser;
        _xpAwardService = xpAwardService;
        _achievementChecker = achievementChecker;
    }

    public async Task<GamifiedResult<DailyLogDto>> Handle(CreateOrUpdateLogCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        var existing = await _repository.GetByDateAsync(request.Date, cancellationToken);

        bool isCreate = existing is null;

        if (existing is null)
        {
            // First log for this date — create
            existing = new DailyLog
            {
                UserId    = userId,
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

        var dto = ToDto(existing);

        if (!isCreate)
            return GamifiedResult<DailyLogDto>.Empty(dto);

        var xpResult = await _xpAwardService.AwardXpAsync(
            userId,
            15,
            XpSource.DailyLog,
            request.Date.ToString("yyyy-MM-dd"),
            cancellationToken);

        var unlocked = await _achievementChecker.CheckAndUnlockAsync(cancellationToken);

        return new GamifiedResult<DailyLogDto>(
            Data: dto,
            XpAwarded: xpResult.XpAwarded,
            NewLevel: xpResult.NewLevel,
            LevelUp: xpResult.LevelUp,
            NewAchievements: unlocked.Select(a => AchievementMapper.ToDto(a, DateTime.UtcNow)).ToList(),
            Idempotent: xpResult.Idempotent);
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