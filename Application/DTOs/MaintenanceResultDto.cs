namespace Application.DTOs;

/// <summary>
///     DTO representing maintenance operation results
/// </summary>
public sealed record MaintenanceResultDto
{
    public bool VacuumCompleted { get; init; }
    public int OrphanedRecordsDeleted { get; init; }
    public int OldCacheEntriesDeleted { get; init; }
    public int TempFilesDeleted { get; init; }
    public bool StatisticsUpdated { get; init; }
    public bool CacheCleared { get; init; }
}