namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Interface for database maintenance operations
/// </summary>
public interface IDatabaseMaintenanceService
{
    /// <summary>
    ///     Vacuums the database to reclaim space and optimize performance
    /// </summary>
    Task VacuumDatabaseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cleans up orphaned records (bookmarks, highlights, etc. for non-existent books)
    /// </summary>
    Task<int> CleanupOrphanedRecordsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Compresses old data that is older than the specified threshold
    /// </summary>
    Task<int> CompressOldDataAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates database statistics for query optimization
    /// </summary>
    Task UpdateStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cleans up temporary files older than the specified number of days
    /// </summary>
    Task<int> CleanupTempFilesAsync(int olderThanDays, CancellationToken cancellationToken = default);
}