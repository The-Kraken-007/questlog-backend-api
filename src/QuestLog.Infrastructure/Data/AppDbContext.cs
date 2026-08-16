using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;

namespace QuestLog.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(200);
            entity.HasIndex(u => u.Email).IsUnique();
        });

        // Habit — scoped to the current user via global query filter
        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).IsRequired().HasMaxLength(100);
            entity.Property(h => h.Emoji).HasMaxLength(10).HasDefaultValue("✅");
            entity.HasOne(h => h.User)
                  .WithMany()
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(h => h.Entries)
                  .WithOne(e => e.Habit)
                  .HasForeignKey(e => e.HabitId)
                  .OnDelete(DeleteBehavior.Cascade);
            // Automatically filter all Habit queries to the current user
            entity.HasQueryFilter(h => h.UserId == _currentUserService.UserId);
        });

        // HabitEntry — unique per habit per date
        modelBuilder.Entity<HabitEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.HabitId, e.Date }).IsUnique();
        });

        // Goal — scoped to the current user via global query filter
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Title).IsRequired().HasMaxLength(200);
            entity.Property(g => g.Status).HasConversion<int>();
            entity.HasOne(g => g.User)
                  .WithMany()
                  .HasForeignKey(g => g.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(g => g.Milestones)
                  .WithOne(m => m.Goal)
                  .HasForeignKey(m => m.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(g => g.UserId == _currentUserService.UserId);
        });

        // Milestone
        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
        });

        // DailyLog — unique per user per date (unique index updated for multi-user)
        modelBuilder.Entity<DailyLog>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasOne(d => d.User)
                  .WithMany()
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            // Unique log per user per day
            entity.HasIndex(d => new { d.UserId, d.Date }).IsUnique();
            entity.Property(d => d.Content).IsRequired();
            entity.HasQueryFilter(d => d.UserId == _currentUserService.UserId);
        });

        // QuestTaskList
        modelBuilder.Entity<QuestTaskList>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(t => t.UserId == _currentUserService.UserId);
        });

        // QuestTask — secured via navigation through the list's UserId
        modelBuilder.Entity<QuestTask>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(t => t.QuestTaskList)
                  .WithMany(l => l.Tasks)
                  .HasForeignKey(t => t.QuestTaskListId)
                  .OnDelete(DeleteBehavior.Cascade);
            // Filter tasks to only those whose parent list belongs to the current user
            entity.HasQueryFilter(t => t.QuestTaskList.UserId == _currentUserService.UserId);
        });

        // UserXp — scoped to the current user via global query filter
        modelBuilder.Entity<UserXp>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(x => x.UserId == _currentUserService.UserId);
        });

        // Achievement — static reference data, no query filter needed
        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Key).IsRequired().HasMaxLength(50);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Description).IsRequired().HasMaxLength(300);
            entity.Property(a => a.Icon).HasMaxLength(10);
            entity.Property(a => a.Category).HasConversion<int>();
            entity.HasIndex(a => a.Key).IsUnique();
        });

        // UserAchievement — scoped to current user, unique per user per achievement
        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(ua => ua.Id);
            entity.HasOne(ua => ua.User)
                  .WithMany()
                  .HasForeignKey(ua => ua.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ua => ua.Achievement)
                  .WithMany()
                  .HasForeignKey(ua => ua.AchievementId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(ua => new { ua.UserId, ua.AchievementId }).IsUnique();
            entity.HasQueryFilter(ua => ua.UserId == _currentUserService.UserId);
        });

        // XpTransaction — audit log of every XP award. UNIQUE composite index on
        // (UserId, Source, ReferenceId) enforces idempotency at the DB level so
        // concurrent duplicate awards are rejected even if the application-level
        // check has a TOCTOU gap. For HabitCompletion the reference id encodes
        // the date (e.g. "5_2026-08-13") so the same index serves daily idempotency.
        modelBuilder.Entity<XpTransaction>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasOne(t => t.User)
                  .WithMany()
                  .HasForeignKey(t => t.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(t => t.Source).HasConversion<int>();
            entity.Property(t => t.ReferenceId).HasMaxLength(100);
            entity.HasIndex(t => new { t.UserId, t.Source, t.ReferenceId }).IsUnique();
            entity.HasQueryFilter(t => t.UserId == _currentUserService.UserId);
        });

        // Seed all 16 achievements with stable GUIDs
        SeedAchievements(modelBuilder);
    }

    private static void SeedAchievements(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Achievement>().HasData(
            // Streaks
            new { Id = Guid.Parse("a1b2c3d4-0001-4000-8000-000000000001"), Key = "first_flame", Name = "First Flame", Description = "Complete any habit for the first time", Category = AchievementCategory.Streak, Icon = "🔥" },
            new { Id = Guid.Parse("a1b2c3d4-0002-4000-8000-000000000002"), Key = "week_warrior", Name = "Week Warrior", Description = "Maintain a 7-day streak on any habit", Category = AchievementCategory.Streak, Icon = "⚔️" },
            new { Id = Guid.Parse("a1b2c3d4-0003-4000-8000-000000000003"), Key = "monthly_master", Name = "Monthly Master", Description = "Maintain a 30-day streak on any habit", Category = AchievementCategory.Streak, Icon = "🛡️" },
            new { Id = Guid.Parse("a1b2c3d4-0004-4000-8000-000000000004"), Key = "century_club", Name = "Century Club", Description = "Maintain a 100-day streak on any habit", Category = AchievementCategory.Streak, Icon = "👑" },
            // Milestones
            new { Id = Guid.Parse("a1b2c3d4-0005-4000-8000-000000000005"), Key = "goal_getter", Name = "Goal Getter", Description = "Complete your first goal", Category = AchievementCategory.Milestone, Icon = "🎯" },
            new { Id = Guid.Parse("a1b2c3d4-0006-4000-8000-000000000006"), Key = "overachiever", Name = "Overachiever", Description = "Complete 10 goals", Category = AchievementCategory.Milestone, Icon = "🏆" },
            new { Id = Guid.Parse("a1b2c3d4-0007-4000-8000-000000000007"), Key = "milestone_marker", Name = "Milestone Marker", Description = "Complete 5 goal milestones", Category = AchievementCategory.Milestone, Icon = "📌" },
            new { Id = Guid.Parse("a1b2c3d4-0008-4000-8000-000000000008"), Key = "perfect_week", Name = "Perfect Week", Description = "Complete at least one habit every day for 7 consecutive days", Category = AchievementCategory.Milestone, Icon = "⭐" },
            // Consistency
            new { Id = Guid.Parse("a1b2c3d4-0009-4000-8000-000000000009"), Key = "dedicated", Name = "Dedicated", Description = "Complete habits 5 days in a week", Category = AchievementCategory.Consistency, Icon = "📅" },
            new { Id = Guid.Parse("a1b2c3d4-0010-4000-8000-000000000010"), Key = "committed", Name = "Committed", Description = "Complete habits 20 days in a month", Category = AchievementCategory.Consistency, Icon = "🗓️" },
            new { Id = Guid.Parse("a1b2c3d4-0011-4000-8000-000000000011"), Key = "unstoppable", Name = "Unstoppable", Description = "Complete habits 100 total times", Category = AchievementCategory.Consistency, Icon = "💪" },
            new { Id = Guid.Parse("a1b2c3d4-0012-4000-8000-000000000012"), Key = "rising_star", Name = "Rising Star", Description = "Reach Level 10", Category = AchievementCategory.Consistency, Icon = "🌟" },
            // Special
            new { Id = Guid.Parse("a1b2c3d4-0013-4000-8000-000000000013"), Key = "night_owl", Name = "Night Owl", Description = "Complete a habit after midnight (UTC)", Category = AchievementCategory.Special, Icon = "🦉" },
            new { Id = Guid.Parse("a1b2c3d4-0014-4000-8000-000000000014"), Key = "early_bird", Name = "Early Bird", Description = "Complete a habit before 7 AM (UTC)", Category = AchievementCategory.Special, Icon = "🐦" },
            new { Id = Guid.Parse("a1b2c3d4-0015-4000-8000-000000000015"), Key = "journal_keeper", Name = "Journal Keeper", Description = "Write 30 daily log entries", Category = AchievementCategory.Special, Icon = "📝" },
            new { Id = Guid.Parse("a1b2c3d4-0016-4000-8000-000000000016"), Key = "level_legend", Name = "Level Legend", Description = "Reach Level 50", Category = AchievementCategory.Special, Icon = "🎖️" }
        );
    }
}
