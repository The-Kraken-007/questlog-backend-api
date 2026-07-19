using QuestLog.Domain.Entities;

namespace QuestLog.Application.QuestTasks.DTOs;

public class QuestTaskDto
{
    public int Id { get; set; }
    public int QuestTaskListId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public static QuestTaskDto FromEntity(QuestTask entity)
    {
        return new QuestTaskDto
        {
            Id = entity.Id,
            QuestTaskListId = entity.QuestTaskListId,
            Name = entity.Name,
            DueDate = entity.DueDate,
            IsCompleted = entity.IsCompleted,
            CreatedAt = entity.CreatedAt
        };
    }
}
