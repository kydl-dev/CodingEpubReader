using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Configuration;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Logs;
using Shared.BackgroundWorkers.Utils;
using Shared.Exceptions;

namespace Shared.BackgroundWorkers;

/// <summary>
///     Background worker that performs routine database maintenance tasks like vacuuming,
///     cleaning up orphaned records, and compressing old data
/// </summary>
public class DatabaseMaintenanceWorker(
    IDatabaseMaintenanceService maintenanceService,
    IDatabaseMaintenanceConfiguration configuration,
    ILogger<DatabaseMaintenanceWorker> logger)
    : BackgroundWorkerBase<DatabaseMaintenanceWorker>(logger)
{
    private readonly IDatabaseMaintenanceConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly IDatabaseMaintenanceService _maintenanceService =
        maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));

    public override string WorkerName => "Database Maintenance Worker";

    // Run daily by default
    protected override TimeSpan ExecutionInterval => _configuration.MaintenanceInterval;

    protected override bool RunImmediatelyOnStartup => false;

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            DatabaseMaintenanceWorkerLogs.Disabled(Logger);
            return;
        }

        DatabaseMaintenanceWorkerLogs.Started(Logger);

        var results = new MaintenanceResults();

        try
        {
            // Vacuum database to reclaim space and optimize performance
            if (_configuration.EnableVacuum)
            {
                DatabaseMaintenanceWorkerLogs.VacuumStarted(Logger);
                await _maintenanceService.VacuumDatabaseAsync(cancellationToken);
                results.VacuumCompleted = true;
                DatabaseMaintenanceWorkerLogs.VacuumCompleted(Logger);
            }

            // Clean up orphaned records
            if (_configuration.EnableOrphanedRecordCleanup)
            {
                DatabaseMaintenanceWorkerLogs.CleanupOrphanedRecordsStarted(Logger);
                results.OrphanedRecordsCleaned =
                    await _maintenanceService.CleanupOrphanedRecordsAsync(cancellationToken);
                DatabaseMaintenanceWorkerLogs.CleanupOrphanedRecordsCompleted(Logger, results.OrphanedRecordsCleaned);
            }

            // Compress old reading history data
            if (_configuration.EnableDataCompression)
            {
                DatabaseMaintenanceWorkerLogs.DataCompressionStarted(Logger);
                var compressionThreshold = DateTime.UtcNow.AddDays(-_configuration.DataCompressionAgeDays);
                results.RecordsCompressed = await _maintenanceService.CompressOldDataAsync(
                    compressionThreshold,
                    cancellationToken);
                DatabaseMaintenanceWorkerLogs.DataCompressionCompleted(Logger, results.RecordsCompressed);
            }

            // Analyze database statistics
            if (_configuration.EnableStatisticsUpdate)
            {
                DatabaseMaintenanceWorkerLogs.StatisticsUpdateStarted(Logger);
                await _maintenanceService.UpdateStatisticsAsync(cancellationToken);
                results.StatisticsUpdated = true;
                DatabaseMaintenanceWorkerLogs.StatisticsUpdateCompleted(Logger);
            }

            // Clean up old temporary files
            if (_configuration.EnableTempFileCleanup)
            {
                DatabaseMaintenanceWorkerLogs.TempFileCleanupStarted(Logger);
                results.TempFilesDeleted = await _maintenanceService.CleanupTempFilesAsync(
                    _configuration.TempFileAgeDays,
                    cancellationToken);
                DatabaseMaintenanceWorkerLogs.TempFileCleanupCompleted(Logger, results.TempFilesDeleted);
            }

            DatabaseMaintenanceWorkerLogs.Completed(Logger, results.ToString());
        }
        catch (Exception ex)
        {
            DatabaseMaintenanceWorkerLogs.ErrorDuringMaintenance(Logger, ex, ex.FullMessage());
            throw;
        }
    }
}