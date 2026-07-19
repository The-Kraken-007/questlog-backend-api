using QuestLog.Domain.Entities;

namespace QuestLog.Application.QuestTasks.DTOs;

public class QuestTaskListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<QuestTaskDto> Tasks { get; set; } = new();

    public static QuestTaskListDto FromEntity(QuestTaskList entity)
    {
        return new QuestTaskListDto
        {
            Id = entity.Id,
            Name = entity.Name,
            SortOrder = entity.SortOrder,
            Tasks = entity.Tasks?.Select(QuestTaskDto.FromEntity).ToList() ?? new List<QuestTaskDto>()
        };
    }
}
