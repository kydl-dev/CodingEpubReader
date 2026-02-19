namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Time series data for log entries
/// </summary>
public class LogTimeSeries
{
    public DateTime Timestamp { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
}