using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;

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
    }
}
