namespace QuestLog.Application.DailyLogs.DTOs;

public class DailyLogDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
