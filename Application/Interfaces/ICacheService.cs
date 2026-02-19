namespace Application.Interfaces;

/// <summary>
///     Interface for caching service operations
///     Enhanced with key tracking for full Clear() and RemoveByPrefix() support
/// </summary>
public interface ICacheService
{
    /// <summary>
    ///     Gets a cached value by key
    /// </summary>
    T? Get<T>(string key);

    /// <summary>
    ///     Sets a value in the cache
    /// </summary>
    void Set<T>(string key, T value, TimeSpan? expiration = null);

    /// <summary>
    ///     Removes a cached value by key
    /// </summary>
    void Remove(string key);

    /// <summary>
    ///     Clears all cached values
    /// </summary>
    void Clear();

    /// <summary>
    ///     Gets or creates a cached value using a factory function
    ///     Thread-safe implementation prevents multiple simultaneous factory executions
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Removes all cached values matching a prefix
    /// </summary>
    void RemoveByPrefix(string prefix);

    /// <summary>
    ///     Gets the current number of cached items (useful for monitoring)
    /// </summary>
    int GetCachedItemsCount();

    /// <summary>
    ///     Gets all cache keys (useful for debugging)
    /// </summary>
    IReadOnlyCollection<string> GetAllKeys();
}