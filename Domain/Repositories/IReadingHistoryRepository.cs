using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories;

public interface IReadingHistoryRepository
{
    Task<ReadingHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ReadingHistory?> GetByBookIdAsync(BookId bookId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ReadingHistory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ReadingHistory>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
    Task<ReadingHistory> AddAsync(ReadingHistory history, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReadingHistory history, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForBookAsync(BookId bookId, CancellationToken cancellationToken = default);
}