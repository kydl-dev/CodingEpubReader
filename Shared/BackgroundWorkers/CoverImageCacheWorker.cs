using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Configuration;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Logs;
using Shared.Exceptions;

namespace Shared.BackgroundWorkers;

/// <summary>
///     Background worker that pre-generates and caches book cover thumbnails in various sizes
///     for better UI performance
/// </summary>
public class CoverImageCacheWorker(
    ICoverImageCacheWorker cacheWorker,
    IBookRepositoryWorker bookRepositoryWorker,
    ICoverImageConfiguration configuration,
    ILogger<CoverImageCacheWorker> logger)
    : BackgroundWorkerBase<CoverImageCacheWorker>(logger)
{
    private readonly IBookRepositoryWorker _bookRepositoryWorker =
        bookRepositoryWorker ?? throw new ArgumentNullException(nameof(bookRepositoryWorker));

    private readonly ICoverImageCacheWorker _cacheWorker =
        cacheWorker ?? throw new ArgumentNullException(nameof(cacheWorker));

    private readonly ICoverImageConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    public override string WorkerName => "Cover Image Cache Worker";

    // Run every hour by default
    protected override TimeSpan ExecutionInterval => _configuration.CacheUpdateInterval;

    protected override bool RunImmediatelyOnStartup => _configuration.GenerateOnStartup;

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            CoverImageCacheWorkerLogs.Disabled(Logger);
            return;
        }

        CoverImageCacheWorkerLogs.Started(Logger);

        try
        {
            // Get all books that need thumbnail generation
            var books = await _bookRepositoryWorker.GetBooksNeedingThumbnailsAsync(cancellationToken);
            var booksList = books.ToList();

            if (!booksList.Any())
            {
                CoverImageCacheWorkerLogs.NoBooksNeedThumbnails(Logger);
                return;
            }

            CoverImageCacheWorkerLogs.GeneratingThumbnails(Logger, booksList.Count);

            var generated = 0;
            var errors = 0;
            var thumbnailSizes = _configuration.GetThumbnailSizes();

            foreach (var book in booksList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var enumerable = thumbnailSizes.ToList();
                    var coverImagePath = book.GetCoverImagePath();
                    if (string.IsNullOrEmpty(coverImagePath) || !File.Exists(coverImagePath))
                    {
                        CoverImageCacheWorkerLogs.BookHasNoCoverImage(Logger, book.Id);
                        continue;
                    }

                    // Generate thumbnails for each configured size
                    foreach (var size in enumerable)
                        await _cacheWorker.GenerateThumbnailAsync(
                            book.Id,
                            coverImagePath,
                            size.Width,
                            size.Height,
                            cancellationToken);

                    generated++;
                    CoverImageCacheWorkerLogs.ThumbnailsGenerated(Logger, book.Id, book.Title);
                }
                catch (Exception ex)
                {
                    errors++;
                    CoverImageCacheWorkerLogs.ErrorGeneratingThumbnails(Logger, ex, book.Id, ex.FullMessage());
                }

                // Add a small delay to avoid overwhelming the system
                if (_configuration.DelayBetweenGenerations > TimeSpan.Zero)
                    await Task.Delay(_configuration.DelayBetweenGenerations, cancellationToken);
            }

            CoverImageCacheWorkerLogs.Completed(Logger, generated, errors);

            // Clean up old cached images
            if (_configuration.EnableCacheCleanup)
            {
                CoverImageCacheWorkerLogs.CleaningUpCache(Logger);
                var deletedCount = await _cacheWorker.CleanupOldCacheAsync(
                    _configuration.CacheMaxAgeDays,
                    cancellationToken);
                CoverImageCacheWorkerLogs.CacheCleanupCompleted(Logger, deletedCount);
            }
        }
        catch (Exception ex)
        {
            CoverImageCacheWorkerLogs.ErrorDuringCacheGeneration(Logger, ex, ex.FullMessage());
            throw;
        }
    }
}