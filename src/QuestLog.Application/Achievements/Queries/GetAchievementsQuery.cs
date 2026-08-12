using MediatR;
using QuestLog.Application.Achievements.DTOs;

namespace QuestLog.Application.Achievements.Queries;

public class GetAchievementsQuery : IRequest<List<AchievementDto>>;
