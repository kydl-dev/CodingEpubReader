using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories;

public interface IHighlightRepository
{
    Task<Highlight?> GetByIdAsync(Guid highlightId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Highlight>> GetByBookIdAsync(BookId bookId, CancellationToken cancellationToken = default);

    Task<IEnumerable<Highlight>> GetByChapterAsync(BookId bookId, string chapterId,
        CancellationToken cancellationToken = default);

    Task<Highlight> AddAsync(Highlight highlight, CancellationToken cancellationToken = default);
    Task UpdateAsync(Highlight highlight, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid highlightId, CancellationToken cancellationToken = default);
}