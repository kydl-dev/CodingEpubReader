using Microsoft.Extensions.Configuration;
using Shared.BackgroundWorkers.Configuration;

namespace Infrastructure.Configuration.WorkersConfigurations;

/// <summary>
///     Configuration for library scanning worker
/// </summary>
public class LibraryScanConfiguration(IConfiguration configuration) : ILibraryScanConfiguration
{
    private const string SectionName = "BackgroundWorkers:LibraryScanning";

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

    public TimeSpan ScanInterval
    {
        get
        {
            var value = _configuration[$"{SectionName}:ScanIntervalMinutes"];
            var minutes = string.IsNullOrEmpty(value) ? 60 : int.Parse(value);
            return TimeSpan.FromMinutes(minutes);
        }
    }

    public bool ScanOnStartup
    {
        get
        {
            var value = _configuration[$"{SectionName}:ScanOnStartup"];
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }
    }

    public List<string?> GetWatchedFolders()
    {
        var section = _configuration.GetSection($"{SectionName}:WatchedFolders");

        return section.GetChildren().Select(child => child.Value).Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
    }
}