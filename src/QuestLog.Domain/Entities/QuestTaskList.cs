namespace QuestLog.Domain.Entities;

public class QuestTaskList
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; } = 0;

    public User User { get; set; } = null!;
    public ICollection<QuestTask> Tasks { get; set; } = new List<QuestTask>();
}
