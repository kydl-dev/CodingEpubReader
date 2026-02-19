namespace Shared.BackgroundWorkers.Utils;

internal class MaintenanceResults
{
    public bool VacuumCompleted { get; set; }
    public int OrphanedRecordsCleaned { get; set; }
    public int RecordsCompressed { get; set; }
    public bool StatisticsUpdated { get; set; }
    public int TempFilesDeleted { get; set; }

    public override string ToString()
    {
        return $"Vacuum: {VacuumCompleted}, Orphaned: {OrphanedRecordsCleaned}, " +
               $"Compressed: {RecordsCompressed}, Stats: {StatisticsUpdated}, " +
               $"TempFiles: {TempFilesDeleted}";
    }
}