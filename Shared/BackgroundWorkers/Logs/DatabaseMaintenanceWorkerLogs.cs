using Microsoft.Extensions.Logging;

namespace Shared.BackgroundWorkers.Logs;

internal static partial class DatabaseMaintenanceWorkerLogs
{
    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 0,
        Level = LogLevel.Debug,
        Message = "Database maintenance is disabled, skipping execution")]
    public static partial void Disabled(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 1,
        Level = LogLevel.Information,
        Message = "Starting database maintenance tasks")]
    public static partial void Started(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 2,
        Level = LogLevel.Information,
        Message = "Running database vacuum")]
    public static partial void VacuumStarted(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 3,
        Level = LogLevel.Information,
        Message = "Database vacuum completed")]
    public static partial void VacuumCompleted(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 4,
        Level = LogLevel.Information,
        Message = "Cleaning up orphaned records")]
    public static partial void CleanupOrphanedRecordsStarted(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 5,
        Level = LogLevel.Information,
        Message = "Cleaned up {Count} orphaned records")]
    public static partial void CleanupOrphanedRecordsCompleted(
        ILogger<DatabaseMaintenanceWorker> logger,
        int count);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 6,
        Level = LogLevel.Information,
        Message = "Compressing old reading history data")]
    public static partial void DataCompressionStarted(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 7,
        Level = LogLevel.Information,
        Message = "Compressed {Count} old records")]
    public static partial void DataCompressionCompleted(
        ILogger<DatabaseMaintenanceWorker> logger,
        int count);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 8,
        Level = LogLevel.Information,
        Message = "Updating database statistics")]
    public static partial void StatisticsUpdateStarted(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 9,
        Level = LogLevel.Information,
        Message = "Database statistics updated")]
    public static partial void StatisticsUpdateCompleted(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 10,
        Level = LogLevel.Information,
        Message = "Cleaning up old temporary files")]
    public static partial void TempFileCleanupStarted(
        ILogger<DatabaseMaintenanceWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 11,
        Level = LogLevel.Information,
        Message = "Deleted {Count} old temporary files")]
    public static partial void TempFileCleanupCompleted(
        ILogger<DatabaseMaintenanceWorker> logger,
        int count);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 12,
        Level = LogLevel.Information,
        Message = "Database maintenance completed. Results: {Results}")]
    public static partial void Completed(
        ILogger<DatabaseMaintenanceWorker> logger,
        string results);

    [LoggerMessage(
        EventId = EventIdRange.DatabaseMaintenanceWorker + 13,
        Level = LogLevel.Error,
        Message = "Error during database maintenance. Error: {Error}")]
    public static partial void ErrorDuringMaintenance(
        ILogger<DatabaseMaintenanceWorker> logger,
        Exception exception,
        string error);
}