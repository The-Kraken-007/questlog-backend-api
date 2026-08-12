using Microsoft.EntityFrameworkCore;
using QuestLog.Domain.Entities;

namespace QuestLog.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the database context. Handlers in the Application layer depend on this
/// interface — never on the concrete AppDbContext — to preserve clean architecture.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Habit> Habits { get; }
    DbSet<HabitEntry> HabitEntries { get; }
    DbSet<Goal> Goals { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<DailyLog> DailyLogs { get; }
    DbSet<QuestTaskList> QuestTaskLists { get; }
    DbSet<QuestTask> QuestTasks { get; }
    DbSet<UserXp> UserXps { get; }
    DbSet<Achievement> Achievements { get; }
    DbSet<UserAchievement> UserAchievements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
