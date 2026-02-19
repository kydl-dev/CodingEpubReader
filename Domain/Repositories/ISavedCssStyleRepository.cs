using Domain.Entities;

namespace Domain.Repositories;

public interface ISavedCssStyleRepository
{
    Task<SavedCssStyle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SavedCssStyle?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<SavedCssStyle?> GetDefaultAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<SavedCssStyle>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<SavedCssStyle> AddAsync(SavedCssStyle style, CancellationToken cancellationToken = default);
    Task UpdateAsync(SavedCssStyle style, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default);
    Task ClearAllDefaultsAsync(CancellationToken cancellationToken = default);
}