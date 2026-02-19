using Microsoft.Extensions.Configuration;
using Shared.BackgroundWorkers.Configuration;
using Shared.BackgroundWorkers.Utils;

namespace Infrastructure.Configuration.WorkersConfigurations;

/// <summary>
///     Configuration for cover image caching worker
/// </summary>
public class CoverImageConfiguration(IConfiguration configuration) : ICoverImageConfiguration
{
    private const string SectionName = "BackgroundWorkers:CoverImageCache";

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

    public TimeSpan CacheUpdateInterval
    {
        get
        {
            var value = _configuration[$"{SectionName}:CacheUpdateIntervalMinutes"];
            var minutes = string.IsNullOrEmpty(value) ? 60 : int.Parse(value);
            return TimeSpan.FromMinutes(minutes);
        }
    }

    public bool GenerateOnStartup
    {
        get
        {
            var value = _configuration[$"{SectionName}:GenerateOnStartup"];
            return !string.IsNullOrEmpty(value) && bool.Parse(value);
        }
    }

    public bool EnableCacheCleanup
    {
        get
        {
            var value = _configuration[$"{SectionName}:EnableCacheCleanup"];
            return string.IsNullOrEmpty(value) || bool.Parse(value);
        }
    }

    public int CacheMaxAgeDays
    {
        get
        {
            var value = _configuration[$"{SectionName}:CacheMaxAgeDays"];
            return string.IsNullOrEmpty(value) ? 30 : int.Parse(value);
        }
    }

    public TimeSpan DelayBetweenGenerations
    {
        get
        {
            var value = _configuration[$"{SectionName}:DelayBetweenGenerationsMs"];
            var milliseconds = string.IsNullOrEmpty(value) ? 100 : int.Parse(value);
            return TimeSpan.FromMilliseconds(milliseconds);
        }
    }

    public IEnumerable<ThumbnailSize> GetThumbnailSizes()
    {
        //var sizes = _configuration.GetSection($"{SectionName}:ThumbnailSizes").Get<List<ThumbnailSizeConfig>>();
        var section = _configuration.GetSection($"{SectionName}:ThumbnailSize");
        var sizes = new List<ThumbnailSize>();

        foreach (var child in section.GetChildren())
        {
            var widthValue = child["Width"];
            var heightValue = child["Height"];

            if (string.IsNullOrEmpty(widthValue) || string.IsNullOrEmpty(heightValue)) continue;
            if (int.TryParse(widthValue, out var width) && int.TryParse(heightValue, out var height))
                sizes.Add(new ThumbnailSize(width, height));
        }

        // Return default sizes if none configured
        if (!sizes.Any())
            return new List<ThumbnailSize>
            {
                new(150, 200), // Small
                new(300, 400), // Medium
                new(600, 800) // Large
            };

        return sizes;
    }
}