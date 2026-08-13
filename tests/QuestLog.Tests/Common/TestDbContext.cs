using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;

namespace QuestLog.Tests.Common;

public class TestDbContext : DbContext, IAppDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitEntry> HabitEntries => Set<HabitEntry>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();
    public DbSet<QuestTaskList> QuestTaskLists => Set<QuestTaskList>();
    public DbSet<QuestTask> QuestTasks => Set<QuestTask>();
    public DbSet<UserXp> UserXps => Set<UserXp>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<XpTransaction> XpTransactions => Set<XpTransaction>();
}
