using Shared.BackgroundWorkers.Utils;

namespace Shared.BackgroundWorkers.Configuration;

/// <summary>
///     Configuration for cover image caching
/// </summary>
public interface ICoverImageConfiguration
{
    bool IsEnabled { get; }
    TimeSpan CacheUpdateInterval { get; }
    bool GenerateOnStartup { get; }
    bool EnableCacheCleanup { get; }
    int CacheMaxAgeDays { get; }
    TimeSpan DelayBetweenGenerations { get; }
    IEnumerable<ThumbnailSize> GetThumbnailSizes();
}