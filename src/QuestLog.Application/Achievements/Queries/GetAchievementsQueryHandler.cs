using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Achievements.DTOs;
using QuestLog.Application.Common.Interfaces;

namespace QuestLog.Application.Achievements.Queries;

public class GetAchievementsQueryHandler : IRequestHandler<GetAchievementsQuery, List<AchievementDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUserService;

    public GetAchievementsQueryHandler(IAppDbContext db, ICurrentUserService currentUserService)
    {
        _db = db;
        _currentUserService = currentUserService;
    }

    public async Task<List<AchievementDto>> Handle(GetAchievementsQuery request, CancellationToken ct)
    {
        var userId = _currentUserService.UserId;

        var achievements = await _db.Achievements.ToListAsync(ct);
        var userAchievements = await _db.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId, ct);

        return achievements.Select(a => new AchievementDto
        {
            Key = a.Key,
            Name = a.Name,
            Description = a.Description,
            Category = a.Category,
            Icon = a.Icon,
            Unlocked = userAchievements.ContainsKey(a.Id),
            UnlockedAt = userAchievements.TryGetValue(a.Id, out var ua) ? ua.UnlockedAt : null
        }).ToList();
    }
}
