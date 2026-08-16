namespace QuestLog.Domain.Entities;

/// <summary>
/// Free-form reflection note a user writes about a calendar week.
/// One per user per week (unique on UserId + WeekStart). The note is
/// upserted by the review feature — saving again updates in place.
/// </summary>
public class WeeklyReflection
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Monday of the week this reflection belongs to.</summary>
    public DateOnly WeekStart { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
