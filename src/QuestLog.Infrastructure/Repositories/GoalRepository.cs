using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Domain.Enums;
using QuestLog.Infrastructure.Data;

namespace QuestLog.Infrastructure.Repositories;

public class GoalRepository : Repository<Goal>, IGoalRepository
{
    public GoalRepository(AppDbContext db) : base(db) { }

    public async Task<List<Goal>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(g => g.Status == GoalStatus.Active)
            .Include(g => g.Milestones)
            .OrderBy(g => g.TargetDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Goal>> GetAllWithMilestonesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.Milestones)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Goal?> GetByIdWithMilestonesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(g => g.Milestones)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Milestone?> GetMilestoneByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Milestones.FindAsync(new object[] { id }, cancellationToken);
    }

    public void AddMilestone(Milestone milestone)
    {
        _db.Milestones.Add(milestone);
    }

    public void UpdateMilestone(Milestone milestone)
    {
        _db.Milestones.Update(milestone);
    }
}
