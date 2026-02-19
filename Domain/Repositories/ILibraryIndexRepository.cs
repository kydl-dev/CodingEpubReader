using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories;

public interface ILibraryIndexRepository
{
    Task<LibraryIndex?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LibraryIndex?> GetByBookIdAsync(BookId bookId, CancellationToken cancellationToken = default);
    Task<IEnumerable<LibraryIndex>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<LibraryIndex>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<IEnumerable<LibraryIndex>> GetFavoritesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<LibraryIndex>> GetRecentlyAccessedAsync(int count = 10,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<LibraryIndex>> GetByTagAsync(string tag, CancellationToken cancellationToken = default);
    Task<LibraryIndex> AddAsync(LibraryIndex index, CancellationToken cancellationToken = default);
    Task UpdateAsync(LibraryIndex index, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsForBookAsync(BookId bookId, CancellationToken cancellationToken = default);
}