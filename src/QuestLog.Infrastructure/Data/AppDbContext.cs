using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;

namespace QuestLog.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitEntry> HabitEntries => Set<HabitEntry>();
    public DbSet<Goal> Goals => Set<Goal>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Habit
        modelBuilder.Entity<Habit>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.Name).IsRequired().HasMaxLength(100);
            entity.Property(h => h.Emoji).HasMaxLength(10).HasDefaultValue("✅");
            entity.HasMany(h => h.Entries)
                  .WithOne(e => e.Habit)
                  .HasForeignKey(e => e.HabitId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // HabitEntry — unique per habit per date
        modelBuilder.Entity<HabitEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.HabitId, e.Date }).IsUnique();
        });

        // Goal
        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Title).IsRequired().HasMaxLength(200);
            entity.Property(g => g.Status).HasConversion<int>();
            entity.HasMany(g => g.Milestones)
                  .WithOne(m => m.Goal)
                  .HasForeignKey(m => m.GoalId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Milestone
        modelBuilder.Entity<Milestone>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
        });

        // DailyLog — unique per date
        modelBuilder.Entity<DailyLog>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.Date).IsUnique();
            entity.Property(d => d.Content).IsRequired();
        });
    }
}
