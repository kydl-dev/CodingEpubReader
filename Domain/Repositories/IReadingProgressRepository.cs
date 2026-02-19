using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories;

public interface IReadingProgressRepository
{
    Task<ReadingPosition?> GetLastPositionAsync(BookId bookId, CancellationToken cancellationToken = default);
    Task SavePositionAsync(ReadingPosition position, CancellationToken cancellationToken = default);
    Task DeleteAsync(BookId bookId, CancellationToken cancellationToken = default);
}