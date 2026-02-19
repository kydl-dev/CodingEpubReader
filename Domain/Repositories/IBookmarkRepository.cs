using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories;

public interface IBookmarkRepository
{
    Task<Bookmark?> GetByIdAsync(Guid bookmarkId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Bookmark>> GetByBookIdAsync(BookId bookId, CancellationToken cancellationToken = default);
    Task<Bookmark> AddAsync(Bookmark bookmark, CancellationToken cancellationToken = default);
    Task UpdateAsync(Bookmark bookmark, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid bookmarkId, CancellationToken cancellationToken = default);
}