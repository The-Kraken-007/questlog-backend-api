using QuestLog.Domain.Enums;

namespace QuestLog.Domain.Entities;

public class Achievement
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AchievementCategory Category { get; set; }
    public string Icon { get; set; } = string.Empty;
}
