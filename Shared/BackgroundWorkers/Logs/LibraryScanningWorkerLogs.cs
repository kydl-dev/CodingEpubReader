using Microsoft.Extensions.Logging;

namespace Shared.BackgroundWorkers.Logs;

internal static partial class LibraryScanningWorkerLogs
{
    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 0,
        Level = LogLevel.Debug,
        Message = "Library scanning is disabled, skipping execution")]
    public static partial void Disabled(
        ILogger<LibraryScanningWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 1,
        Level = LogLevel.Debug,
        Message = "No watched folders configured, skipping library scan")]
    public static partial void NoWatchedFolders(
        ILogger<LibraryScanningWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 2,
        Level = LogLevel.Information,
        Message = "Starting library scan of {FolderCount} watched folders")]
    public static partial void Started(
        ILogger<LibraryScanningWorker> logger,
        int folderCount);

    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 3,
        Level = LogLevel.Warning,
        Message = "Watched folder does not exist: {Folder}")]
    public static partial void FolderNotFound(
        ILogger<LibraryScanningWorker> logger,
        string folder);

    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 4,
        Level = LogLevel.Information,
        Message = "Scanning folder: {Folder}")]
    public static partial void ScanningFolder(
        ILogger<LibraryScanningWorker> logger,
        string folder);

    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 5,
        Level = LogLevel.Information,
        Message = "Imported {Count} books from folder: {Folder}")]
    public static partial void FolderImportCompleted(
        ILogger<LibraryScanningWorker> logger,
        int count,
        string folder);

    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 6,
        Level = LogLevel.Error,
        Message = "Error scanning folder: {Folder}. Error: {Error}")]
    public static partial void ErrorScanningFolder(
        ILogger<LibraryScanningWorker> logger,
        Exception exception,
        string folder,
        string error);

    [LoggerMessage(
        EventId = EventIdRange.LibraryScanWorker + 7,
        Level = LogLevel.Information,
        Message = "Library scan completed. Total imported: {TotalImported}, Total errors: {TotalErrors}")]
    public static partial void Completed(
        ILogger<LibraryScanningWorker> logger,
        int totalImported,
        int totalErrors);
}