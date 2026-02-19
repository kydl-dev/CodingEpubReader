using System.Collections.Concurrent;
using Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
///     Enhanced cache service with proper key tracking for Clear and RemoveByPrefix operations
/// </summary>
public class CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger) : ICacheService, IDisposable
{
    private readonly SemaphoreSlim _clearLock = new(1, 1);

    private readonly MemoryCacheEntryOptions _defaultCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
        SlidingExpiration = TimeSpan.FromMinutes(10)
    };

    private readonly ConcurrentDictionary<string, byte> _keyTracker = new();
    private readonly ILogger<CacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMemoryCache _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));

    public T? Get<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        if (_memoryCache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return value;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        var cacheOptions = expiration.HasValue
            ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration.Value }
            : _defaultCacheOptions;

        // Register callback to remove key from tracker when evicted
        cacheOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            _keyTracker.TryRemove(evictedKey.ToString()!, out _);
            _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
        });

        _memoryCache.Set(key, value, cacheOptions);
        _keyTracker.TryAdd(key, 0); // Track the key (value doesn't matter, using ConcurrentDictionary as a set)

        _logger.LogDebug("Cached value for key: {Key}", key);
    }

    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        _memoryCache.Remove(key);
        _keyTracker.TryRemove(key, out _);

        _logger.LogDebug("Removed cache entry for key: {Key}", key);
    }

    public void Clear()
    {
        _clearLock.Wait();
        try
        {
            var keys = _keyTracker.Keys.ToList();
            var removedCount = 0;

            foreach (var key in keys)
            {
                _memoryCache.Remove(key);
                _keyTracker.TryRemove(key, out _);
                removedCount++;
            }

            _logger.LogInformation("Cache cleared. Removed {Count} entries", removedCount);
        }
        finally
        {
            _clearLock.Release();
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be empty", nameof(key));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        if (_memoryCache.TryGetValue(key, out T? cachedValue))
        {
            _logger.LogDebug("Cache hit for key: {Key}", key);
            return cachedValue!;
        }

        _logger.LogDebug("Cache miss for key: {Key}, creating new value", key);
        var value = await factory();

        var cacheOptions = expiration.HasValue
            ? new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration.Value }
            : _defaultCacheOptions;

        // Register callback to remove key from tracker when evicted
        cacheOptions.RegisterPostEvictionCallback((evictedKey, evictedValue, reason, state) =>
        {
            _keyTracker.TryRemove(evictedKey.ToString()!, out _);
            _logger.LogDebug("Cache entry evicted: {Key}, Reason: {Reason}", evictedKey, reason);
        });

        _memoryCache.Set(key, value, cacheOptions);
        _keyTracker.TryAdd(key, 0);

        _logger.LogDebug("Cached new value for key: {Key}", key);

        return value;
    }

    public void RemoveByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return;

        var keysToRemove = _keyTracker.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (!keysToRemove.Any())
        {
            _logger.LogDebug("No keys found matching prefix: {Prefix}", prefix);
            return;
        }

        var removedCount = 0;
        foreach (var key in keysToRemove)
        {
            _memoryCache.Remove(key);
            _keyTracker.TryRemove(key, out _);
            removedCount++;
        }

        _logger.LogInformation("Removed {Count} cache entries with prefix: {Prefix}", removedCount, prefix);
    }

    /// <summary>
    ///     Gets the current number of cached items
    /// </summary>
    public int GetCachedItemsCount()
    {
        return _keyTracker.Count;
    }

    /// <summary>
    ///     Gets all cache keys (useful for debugging)
    /// </summary>
    public IReadOnlyCollection<string> GetAllKeys()
    {
        return _keyTracker.Keys.ToList();
    }

    public void Dispose()
    {
        _clearLock.Dispose();
        GC.SuppressFinalize(this);
    }
}