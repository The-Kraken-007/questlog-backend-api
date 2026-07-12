using Microsoft.EntityFrameworkCore;
using QuestLog.Application.Common.Interfaces;
using QuestLog.Domain.Entities;
using QuestLog.Infrastructure.Data;

namespace QuestLog.Infrastructure.Repositories;

public class HabitRepository : Repository<Habit>, IHabitRepository
{
    public HabitRepository(AppDbContext db) : base(db) { }

    public async Task<List<Habit>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => !h.IsArchived)
            .OrderBy(h => h.SortOrder)
            .Include(h => h.Entries)
            .ToListAsync(cancellationToken);
    }

    public async Task<Habit?> GetByIdWithEntriesAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(h => h.Entries)
            .FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(h => !h.IsArchived)
            .Select(h => (int?)h.SortOrder)
            .MaxAsync(cancellationToken) ?? 0;
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(h => h.Id == id, cancellationToken);
    }

    public async Task<HabitEntry?> GetEntryAsync(int habitId, DateOnly date, CancellationToken cancellationToken = default)
    {
        return await _db.HabitEntries
            .FirstOrDefaultAsync(e => e.HabitId == habitId && e.Date == date, cancellationToken);
    }

    public async Task<List<HabitEntry>> GetEntriesAsync(int habitId, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default)
    {
        var query = _db.HabitEntries.Where(e => e.HabitId == habitId);

        if (startDate.HasValue)
            query = query.Where(e => e.Date >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(e => e.Date <= endDate.Value);

        return await query.OrderByDescending(e => e.Date).ToListAsync(cancellationToken);
    }

    public async Task<List<DateOnly>> GetAllCompletedDatesAsync(int habitId, CancellationToken cancellationToken = default)
    {
        return await _db.HabitEntries
            .Where(e => e.HabitId == habitId && e.IsCompleted)
            .Select(e => e.Date)
            .ToListAsync(cancellationToken);
    }

    public void AddEntry(HabitEntry entry)
    {
        _db.HabitEntries.Add(entry);
    }

    public void UpdateEntry(HabitEntry entry)
    {
        _db.HabitEntries.Update(entry);
    }
}
