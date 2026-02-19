using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;
using Shared.Exceptions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Infrastructure.Services;

/// <summary>
///     Service for caching book cover thumbnails
/// </summary>
public class CoverImageCacheService : ICoverImageCacheWorker
{
    private readonly string _cacheDirectory;
    private readonly ILogger<CoverImageCacheService> _logger;

    public CoverImageCacheService(ILogger<CoverImageCacheService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set up cache directory (you may want to make this configurable)
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _cacheDirectory = Path.Combine(appDataPath, "EpubReader", "CoverCache");

        // Ensure cache directory exists
        if (Directory.Exists(_cacheDirectory)) return;
        Directory.CreateDirectory(_cacheDirectory);
        _logger.LogInformation("Created cover cache directory: {Directory}", _cacheDirectory);
    }

    public async Task GenerateThumbnailAsync(
        Guid bookId,
        string sourcePath,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(sourcePath))
            {
                _logger.LogWarning("Source cover image not found: {Path}", sourcePath);
                return;
            }

            var thumbnailPath = GetThumbnailPath(bookId, width, height);

            // Check if thumbnail already exists
            if (File.Exists(thumbnailPath))
            {
                _logger.LogDebug("Thumbnail already exists: {Path}", thumbnailPath);
                return;
            }

            _logger.LogDebug("Generating thumbnail for book {BookId} at size {Width}x{Height}",
                bookId, width, height);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(thumbnailPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

            // Load and resize image using ImageSharp
            using var image = await Image.LoadAsync(sourcePath, cancellationToken);

            // Resize maintaining aspect ratio
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(width, height),
                Mode = ResizeMode.Max
            }));

            // Save the thumbnail
            await image.SaveAsync(thumbnailPath, cancellationToken);

            _logger.LogDebug("Generated thumbnail: {Path}", thumbnailPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for book {BookId}. Error: {Error}",
                bookId, ex.FullMessage());
            throw;
        }
    }

    public async Task<byte[]?> GetThumbnailAsync(
        Guid bookId,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var thumbnailPath = GetThumbnailPath(bookId, width, height);

            if (File.Exists(thumbnailPath)) return await File.ReadAllBytesAsync(thumbnailPath, cancellationToken);
            _logger.LogDebug("Thumbnail not found for book {BookId} at size {Width}x{Height}",
                bookId, width, height);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving thumbnail for book {BookId}. Error: {Error}",
                bookId, ex.FullMessage());
            return null;
        }
    }

    public Task<int> CleanupOldCacheAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of cached images older than {Days} days", olderThanDays);
            var deletedCount = 0;

            if (!Directory.Exists(_cacheDirectory))
            {
                _logger.LogDebug("Cache directory does not exist");
                return Task.FromResult(0);
            }

            var threshold = DateTime.UtcNow.AddDays(-olderThanDays);
            var files = Directory.GetFiles(_cacheDirectory, "*.jpg", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_cacheDirectory, "*.png", SearchOption.AllDirectories))
                .ToList();

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastAccessTimeUtc < threshold)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted old cached image: {File}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cached image: {File}", file);
                }
            }

            _logger.LogInformation("Cleaned up {Count} old cached images", deletedCount);
            return Task.FromResult(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cache cleanup. Error: {Error}", ex.FullMessage());
            throw;
        }
    }

    public async Task InvalidateCacheAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Invalidating cache for book {BookId}", bookId);

            var bookCacheDirectory = Path.Combine(_cacheDirectory, bookId.ToString());

            if (Directory.Exists(bookCacheDirectory))
            {
                Directory.Delete(bookCacheDirectory, true);
                _logger.LogInformation("Deleted cache directory for book {BookId}", bookId);
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating cache for book {BookId}. Error: {Error}",
                bookId, ex.FullMessage());
            throw;
        }
    }

    private string GetThumbnailPath(Guid bookId, int width, int height)
    {
        var bookDirectory = Path.Combine(_cacheDirectory, bookId.ToString());
        return Path.Combine(bookDirectory, $"thumb_{width}x{height}.jpg");
    }
}