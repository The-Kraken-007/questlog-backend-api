using QuestLog.Domain.Entities;

namespace QuestLog.Application.Common.Interfaces;

public interface IGoalRepository : IRepository<Goal>
{
    Task<List<Goal>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<List<Goal>> GetAllWithMilestonesAsync(CancellationToken cancellationToken = default);
    Task<Goal?> GetByIdWithMilestonesAsync(int id, CancellationToken cancellationToken = default);
    
    // Milestone methods
    Task<Milestone?> GetMilestoneByIdAsync(int id, CancellationToken cancellationToken = default);
    void AddMilestone(Milestone milestone);
    void UpdateMilestone(Milestone milestone);
}
