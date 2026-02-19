using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SettingsRepository(EpubReaderDbContext context) : ISettingsRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return await _context.Settings
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task<IEnumerable<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Settings
            .OrderBy(s => s.Key)
            .ToListAsync(cancellationToken);
    }

    public async Task<Setting> AddAsync(Setting setting, CancellationToken cancellationToken = default)
    {
        if (setting == null)
            throw new ArgumentNullException(nameof(setting));

        setting.LastUpdated = DateTime.UtcNow;
        await _context.Settings.AddAsync(setting, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return setting;
    }

    public async Task UpdateAsync(Setting setting, CancellationToken cancellationToken = default)
    {
        if (setting == null)
            throw new ArgumentNullException(nameof(setting));

        setting.LastUpdated = DateTime.UtcNow;
        _context.Settings.Update(setting);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var setting = await GetByKeyAsync(key, cancellationToken);
        if (setting != null)
        {
            _context.Settings.Remove(setting);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));

        return await _context.Settings
            .AnyAsync(s => s.Key == key, cancellationToken);
    }

    public async Task UpsertAsync(Setting setting, CancellationToken cancellationToken = default)
    {
        if (setting == null)
            throw new ArgumentNullException(nameof(setting));

        var existing = await GetByKeyAsync(setting.Key, cancellationToken);

        if (existing != null)
        {
            existing.Value = setting.Value;
            existing.Description = setting.Description;
            await UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await AddAsync(setting, cancellationToken);
        }
    }
}