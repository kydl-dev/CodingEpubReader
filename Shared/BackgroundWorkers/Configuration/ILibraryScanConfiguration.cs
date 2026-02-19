namespace Shared.BackgroundWorkers.Configuration;

/// <summary>
///     Configuration interface for library scanning
/// </summary>
public interface ILibraryScanConfiguration
{
    /// <summary>
    ///     Gets whether library scanning is enabled
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    ///     Gets the interval between scans
    /// </summary>
    TimeSpan ScanInterval { get; }

    /// <summary>
    ///     Gets whether to scan immediately on startup
    /// </summary>
    bool ScanOnStartup { get; }

    /// <summary>
    ///     Gets the list of folders to watch for new EPUB files
    /// </summary>
    List<string?> GetWatchedFolders();
}