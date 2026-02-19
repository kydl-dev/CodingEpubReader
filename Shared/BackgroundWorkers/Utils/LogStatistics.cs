namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Aggregated log statistics
/// </summary>
public class LogStatistics
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; set; }
    public TimeSpan ProcessingDuration { get; set; }
    public int ProcessedFiles { get; set; }
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public int TotalInformation { get; set; }
    public Dictionary<string, int> ErrorsByType { get; init; } = new();
    public Dictionary<string, ErrorMessageInfo> ErrorsByMessage { get; init; } = new();
    public Dictionary<string, ErrorMessageInfo> TopErrors { get; set; } = new();
    public Dictionary<DateTime, LogTimeSeries> TimeSeriesData { get; init; } = new();
}