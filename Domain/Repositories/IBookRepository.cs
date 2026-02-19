using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Repositories;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(BookId id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task<Book> AddAsync(Book book, CancellationToken cancellationToken = default);
    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
    Task DeleteAsync(BookId id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(BookId id, CancellationToken cancellationToken = default);
}