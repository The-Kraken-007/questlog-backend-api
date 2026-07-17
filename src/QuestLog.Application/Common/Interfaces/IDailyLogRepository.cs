using QuestLog.Domain.Entities;

namespace QuestLog.Application.Common.Interfaces;

public interface IDailyLogRepository : IRepository<DailyLog>
{
    Task<DailyLog?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<List<DailyLog>> GetLogsInRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
