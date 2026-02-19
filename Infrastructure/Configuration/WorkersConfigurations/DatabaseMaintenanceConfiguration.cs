using Microsoft.Extensions.Configuration;
using Shared.BackgroundWorkers.Configuration;

namespace Infrastructure.Configuration.WorkersConfigurations;

/// <summary>
///     Configuration for database maintenance worker
/// </summary>
public class DatabaseMaintenanceConfiguration(IConfiguration configuration) : IDatabaseMaintenanceConfiguration
{
    private const string SectionName = "BackgroundWorkers:DatabaseMaintenance";

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

    public TimeSpan MaintenanceInterval
    {
        get
        {
            var value = _configuration[$"{SectionName}:MaintenanceIntervalHours"];
            var hours = string.IsNullOrEmpty(value) ? 24 : int.Parse(value);
            return TimeSpan.FromHours(hours);
        }
    }

    public bool EnableVacuum
    {
        get
        {
            var value = _configuration[$"{SectionName}:EnableVacuum"];
            return string.IsNullOrEmpty(value) || bool.Parse(value);
        }
    }

    public bool EnableOrphanedRecordCleanup
    {
        get
        {
            var value = _configuration[$"{SectionName}:EnableOrphanedRecordCleanup"];
            return string.IsNullOrEmpty(value) || bool.Parse(value);
        }
    }

    public bool EnableDataCompression
    {
        get
        {
            var value = _configuration[$"{SectionName}:EnableDataCompression"];
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }
    }

    public int DataCompressionAgeDays
    {
        get
        {
            var value = _configuration[$"{SectionName}:DataCompressionAgeDays"];
            return string.IsNullOrEmpty(value) ? 90 : int.Parse(value);
        }
    }

    public bool EnableStatisticsUpdate
    {
        get
        {
            var value = _configuration[$"{SectionName}:EnableStatisticsUpdate"];
            return string.IsNullOrEmpty(value) || bool.Parse(value);
        }
    }

    public bool EnableTempFileCleanup
    {
        get
        {
            var value = _configuration[$"{SectionName}:EnableTempFileCleanup"];
            return string.IsNullOrEmpty(value) || bool.Parse(value);
        }
    }

    public int TempFileAgeDays
    {
        get
        {
            var value = _configuration[$"{SectionName}:TempFileAgeDays"];
            return string.IsNullOrEmpty(value) ? 7 : int.Parse(value);
        }
    }
}