using Domain.Entities;

namespace Domain.Repositories;

public interface ILibraryRepository
{
    Task<Library?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Library?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Library?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Library>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Library> AddAsync(Library library, CancellationToken cancellationToken = default);
    Task UpdateAsync(Library library, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
}