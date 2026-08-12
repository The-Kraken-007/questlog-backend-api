namespace QuestLog.Domain.Entities;

public class UserXp
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public int TotalXp { get; set; }
    public int CurrentLevel { get; set; }
    public DateTime UpdatedAt { get; set; }
}
