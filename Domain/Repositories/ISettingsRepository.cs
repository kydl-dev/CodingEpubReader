using Domain.Entities;

namespace Domain.Repositories;

public interface ISettingsRepository
{
    Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<IEnumerable<Setting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Setting> AddAsync(Setting setting, CancellationToken cancellationToken = default);
    Task UpdateAsync(Setting setting, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(Setting setting, CancellationToken cancellationToken = default);
}