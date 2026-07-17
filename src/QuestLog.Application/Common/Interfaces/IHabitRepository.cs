using QuestLog.Domain.Entities;

namespace QuestLog.Application.Common.Interfaces;

public interface IHabitRepository : IRepository<Habit>
{
    Task<List<Habit>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Habit?> GetByIdWithEntriesAsync(int id, CancellationToken cancellationToken = default);
    Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    
    // Habit Entry methods
    Task<HabitEntry?> GetEntryAsync(int habitId, DateOnly date, CancellationToken cancellationToken = default);
    Task<List<HabitEntry>> GetEntriesAsync(int habitId, DateOnly? startDate, DateOnly? endDate, CancellationToken cancellationToken = default);
    Task<List<DateOnly>> GetAllCompletedDatesAsync(int habitId, CancellationToken cancellationToken = default);
    void AddEntry(HabitEntry entry);
    void UpdateEntry(HabitEntry entry);
}
