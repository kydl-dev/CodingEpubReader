using Microsoft.Extensions.Logging;

namespace Shared.BackgroundWorkers.Logs;

internal static partial class LoggingStatisticsWorkerLogs
{
    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 0,
        Level = LogLevel.Debug,
        Message = "Logging statistics aggregation is disabled, skipping execution")]
    public static partial void Disabled(
        ILogger<LoggingStatisticsWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 1,
        Level = LogLevel.Information,
        Message = "Starting log statistics aggregation")]
    public static partial void Started(
        ILogger<LoggingStatisticsWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 2,
        Level = LogLevel.Debug,
        Message = "No log files found for processing")]
    public static partial void NoLogFilesFound(
        ILogger<LoggingStatisticsWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 3,
        Level = LogLevel.Information,
        Message = "Processing {FileCount} log files")]
    public static partial void ProcessingFiles(
        ILogger<LoggingStatisticsWorker> logger,
        int fileCount);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 4,
        Level = LogLevel.Error,
        Message = "Error processing log file {FilePath}. Error: {Error}")]
    public static partial void ErrorProcessingLogFile(
        ILogger<LoggingStatisticsWorker> logger,
        Exception exception,
        string filePath,
        string error);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 5,
        Level = LogLevel.Information,
        Message =
            "Log statistics aggregation completed. Files: {Files}, Errors: {Errors}, Warnings: {Warnings}, Info: {Info}, Duration: {Duration}")]
    public static partial void Completed(
        ILogger<LoggingStatisticsWorker> logger,
        int files,
        int errors,
        int warnings,
        int info,
        TimeSpan duration);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 6,
        Level = LogLevel.Error,
        Message = "Error during log statistics aggregation. Error: {Error}")]
    public static partial void ErrorDuringAggregation(
        ILogger<LoggingStatisticsWorker> logger,
        Exception exception,
        string error);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 7,
        Level = LogLevel.Warning,
        Message = "Log file not found: {FilePath}")]
    public static partial void LogFileNotFound(
        ILogger<LoggingStatisticsWorker> logger,
        string filePath);

    [LoggerMessage(
        EventId = EventIdRange.LoggingStatisticsWorker + 8,
        Level = LogLevel.Debug,
        Message = "Processing log file: {FilePath}")]
    public static partial void ProcessingLogFile(
        ILogger<LoggingStatisticsWorker> logger,
        string filePath);
}