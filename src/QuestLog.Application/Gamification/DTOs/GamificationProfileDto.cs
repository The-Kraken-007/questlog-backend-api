namespace QuestLog.Application.Gamification.DTOs;

public class GamificationProfileDto
{
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; }
    public string Title { get; set; } = string.Empty;
    public int XpInCurrentLevel { get; set; }
    public int XpForCurrentLevel { get; set; }
    public int XpToNextLevel { get; set; }
}
