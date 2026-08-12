using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Application.Common.Services;
using QuestLog.Application.Gamification.DTOs;

namespace QuestLog.Application.Gamification.Queries;

public class GetGamificationProfileQueryHandler : IRequestHandler<GetGamificationProfileQuery, GamificationProfileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetGamificationProfileQueryHandler(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<GamificationProfileDto> Handle(GetGamificationProfileQuery request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;
        var userXp = await _db.UserXps
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        var totalXp = userXp?.TotalXp ?? 0;
        var level = LevelCalculator.CalculateLevel(totalXp);

        return new GamificationProfileDto
        {
            TotalXp = totalXp,
            CurrentLevel = level,
            Title = LevelCalculator.GetTitle(level),
            XpInCurrentLevel = LevelCalculator.GetXpInCurrentLevel(totalXp),
            XpForCurrentLevel = LevelCalculator.GetXpForLevel(level),
            XpToNextLevel = LevelCalculator.GetXpToNextLevel(totalXp)
        };
    }
}
