namespace QuestLog.Domain.Entities;

public class QuestTask
{
    public int Id { get; set; }
    public int QuestTaskListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public QuestTaskList QuestTaskList { get; set; } = null!;
}
