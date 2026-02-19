using System.Text.RegularExpressions;

namespace Shared.BackgroundWorkers.Configuration;

/// <summary>
///     Configuration for logging statistics
/// </summary>
public interface ILoggingStatisticsConfiguration
{
    bool IsEnabled { get; }
    TimeSpan AggregationInterval { get; }
    int LogRetentionDays { get; }
    int TopErrorsCount { get; }
    Regex GetLogEntryPattern();
}