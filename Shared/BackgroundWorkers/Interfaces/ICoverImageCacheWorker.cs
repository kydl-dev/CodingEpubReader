namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Interface for cover image caching operations
/// </summary>
public interface ICoverImageCacheWorker
{
    /// <summary>
    ///     Generates a thumbnail for a book cover at the specified size
    /// </summary>
    Task GenerateThumbnailAsync(
        Guid bookId,
        string sourcePath,
        int width,
        int height,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets a cached thumbnail for a book at the specified size
    /// </summary>
    Task<byte[]?> GetThumbnailAsync(
        Guid bookId,
        int width,
        int height,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cleans up cached images older than the specified number of days
    /// </summary>
    Task<int> CleanupOldCacheAsync(int olderThanDays, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Invalidates the cache for a specific book
    /// </summary>
    Task InvalidateCacheAsync(Guid bookId, CancellationToken cancellationToken = default);
}