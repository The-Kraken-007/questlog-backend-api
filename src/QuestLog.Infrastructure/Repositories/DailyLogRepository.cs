using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Infrastructure.Data;

namespace QuestLog.Infrastructure.Repositories;

public class DailyLogRepository : Repository<DailyLog>, IDailyLogRepository
{
    public DailyLogRepository(AppDbContext db) : base(db) { }

    public async Task<DailyLog?> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.Date == date, cancellationToken);
    }

    public async Task<List<DailyLog>> GetLogsInRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(d => d.Date >= startDate && d.Date <= endDate)
            .OrderBy(d => d.Date)
            .ToListAsync(cancellationToken);
    }
}
