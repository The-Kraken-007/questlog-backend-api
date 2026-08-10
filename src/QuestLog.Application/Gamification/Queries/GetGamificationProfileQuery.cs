using MediatR;
using QuestLog.Application.Gamification.DTOs;

namespace QuestLog.Application.Gamification.Queries;

public class GetGamificationProfileQuery : IRequest<GamificationProfileDto>;
