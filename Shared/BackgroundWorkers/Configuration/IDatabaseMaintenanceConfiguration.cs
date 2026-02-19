namespace Shared.BackgroundWorkers.Configuration;

/// <summary>
///     Configuration for database maintenance
/// </summary>
public interface IDatabaseMaintenanceConfiguration
{
    bool IsEnabled { get; }
    TimeSpan MaintenanceInterval { get; }
    bool EnableVacuum { get; }
    bool EnableOrphanedRecordCleanup { get; }
    bool EnableDataCompression { get; }
    int DataCompressionAgeDays { get; }
    bool EnableStatisticsUpdate { get; }
    bool EnableTempFileCleanup { get; }
    int TempFileAgeDays { get; }
}