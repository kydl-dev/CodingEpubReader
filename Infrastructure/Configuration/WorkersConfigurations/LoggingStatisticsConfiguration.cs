using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Shared.BackgroundWorkers.Configuration;

namespace Infrastructure.Configuration.WorkersConfigurations;

/// <summary>
///     Configuration for logging statistics worker
/// </summary>
public class LoggingStatisticsConfiguration(IConfiguration configuration) : ILoggingStatisticsConfiguration
{
    private const string SectionName = "BackgroundWorkers:LoggingStatistics";

    private readonly IConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    public bool IsEnabled
    {
        get
        {
            var value = _configuration[$"{SectionName}:IsEnabled"];
            return string.IsNullOrEmpty(value) || bool.Parse(value);
        }
    }

    public TimeSpan AggregationInterval
    {
        get
        {
            var value = _configuration[$"{SectionName}:AggregationIntervalMinutes"];
            var minutes = string.IsNullOrEmpty(value) ? 15 : int.Parse(value);
            return TimeSpan.FromMinutes(minutes);
        }
    }

    public int LogRetentionDays
    {
        get
        {
            var value = _configuration[$"{SectionName}:LogRetentionDays"];
            return string.IsNullOrEmpty(value) ? 30 : int.Parse(value);
        }
    }

    public int TopErrorsCount
    {
        get
        {
            var value = _configuration[$"{SectionName}:TopErrorsCount"];
            return string.IsNullOrEmpty(value) ? 20 : int.Parse(value);
        }
    }

    public Regex GetLogEntryPattern()
    {
        // Default pattern for ASP.NET Core structured logging format
        // Example: 2026-02-13 14:30:45.123 +00:00 [INF] Message here
        var pattern = _configuration[$"{SectionName}:LogEntryPattern"];

        if (string.IsNullOrWhiteSpace(pattern))
            pattern =
                @"(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d{3})?(?:\s+[+-]\d{2}:\d{2})?)\s+\[(?<level>\w+)\]\s+(?<message>.+?)(?:\s+(?<exception>System\..+))?$";

        return new Regex(pattern, RegexOptions.Compiled | RegexOptions.Multiline);
    }
}