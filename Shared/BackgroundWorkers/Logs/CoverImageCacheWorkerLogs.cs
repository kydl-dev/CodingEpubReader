using Microsoft.Extensions.Logging;

namespace Shared.BackgroundWorkers.Logs;

internal static partial class CoverImageCacheWorkerLogs
{
    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 0,
        Level = LogLevel.Debug,
        Message = "Cover image caching is disabled, skipping execution")]
    public static partial void Disabled(
        ILogger<CoverImageCacheWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 1,
        Level = LogLevel.Information,
        Message = "Starting cover image cache generation")]
    public static partial void Started(
        ILogger<CoverImageCacheWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 2,
        Level = LogLevel.Debug,
        Message = "No books need thumbnail generation")]
    public static partial void NoBooksNeedThumbnails(
        ILogger<CoverImageCacheWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 3,
        Level = LogLevel.Information,
        Message = "Generating thumbnails for {BookCount} books")]
    public static partial void GeneratingThumbnails(
        ILogger<CoverImageCacheWorker> logger,
        int bookCount);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 4,
        Level = LogLevel.Debug,
        Message = "Book {BookId} has no cover image, skipping")]
    public static partial void BookHasNoCoverImage(
        ILogger<CoverImageCacheWorker> logger,
        Guid bookId);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 5,
        Level = LogLevel.Debug,
        Message = "Generated thumbnails for book {BookId}: {Title}")]
    public static partial void ThumbnailsGenerated(
        ILogger<CoverImageCacheWorker> logger,
        Guid bookId,
        string title);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 6,
        Level = LogLevel.Error,
        Message = "Error generating thumbnails for book {BookId}. Error: {Error}")]
    public static partial void ErrorGeneratingThumbnails(
        ILogger<CoverImageCacheWorker> logger,
        Exception exception,
        Guid bookId,
        string error);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 7,
        Level = LogLevel.Information,
        Message = "Cover image cache generation completed. Generated: {Generated}, Errors: {Errors}")]
    public static partial void Completed(
        ILogger<CoverImageCacheWorker> logger,
        int generated,
        int errors);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 8,
        Level = LogLevel.Information,
        Message = "Cleaning up old cached images")]
    public static partial void CleaningUpCache(
        ILogger<CoverImageCacheWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 9,
        Level = LogLevel.Information,
        Message = "Deleted {Count} old cached images")]
    public static partial void CacheCleanupCompleted(
        ILogger<CoverImageCacheWorker> logger,
        int count);

    [LoggerMessage(
        EventId = EventIdRange.CoverImageCacheWorker + 10,
        Level = LogLevel.Error,
        Message = "Error during cover image cache generation. Error: {Error}")]
    public static partial void ErrorDuringCacheGeneration(
        ILogger<CoverImageCacheWorker> logger,
        Exception exception,
        string error);
}