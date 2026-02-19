using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Utils;
using Shared.Exceptions;

namespace Infrastructure.Services;

/// <summary>
///     Service for caching and managing log statistics
/// </summary>
public class LogStatisticsService(
    IMemoryCache cache,
    ILogger<LogStatisticsService> logger)
    : ILogStatisticsService
{
    private const string CacheKey = "LogStatistics";
    private const string ErrorSearchCacheKeyPrefix = "ErrorSearch_";
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<LogStatisticsService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task CacheStatisticsAsync(LogStatistics statistics, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Caching log statistics");

            // Cache the statistics for 1 hour
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(30)
            };

            _cache.Set(CacheKey, statistics, cacheOptions);

            _logger.LogInformation("Cached log statistics: Errors={Errors}, Warnings={Warnings}, Info={Info}",
                statistics.TotalErrors, statistics.TotalWarnings, statistics.TotalInformation);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching log statistics. Error: {Error}", ex.FullMessage());
            throw;
        }
    }

    public async Task<LogStatistics?> GetCachedStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cache.TryGetValue(CacheKey, out LogStatistics? statistics))
            {
                _logger.LogDebug("Retrieved cached log statistics");
                return statistics;
            }

            _logger.LogDebug("No cached log statistics found");
            return await Task.FromResult<LogStatistics?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cached log statistics. Error: {Error}", ex.FullMessage());
            return null;
        }
    }

    public async Task<IEnumerable<ErrorMessageInfo>> SearchErrorsAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Searching errors for term: {SearchTerm}", searchTerm);

            // Get cached statistics
            var statistics = await GetCachedStatisticsAsync(cancellationToken);
            if (statistics == null)
            {
                _logger.LogWarning("No cached statistics available for search");
                return [];
            }

            // Search in error messages
            var results = statistics.ErrorsByMessage.Values
                .Where(e => e.Message.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            e.ErrorType.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(e => e.Count)
                .Take(50) // Limit to top 50 results
                .ToList();

            _logger.LogDebug("Found {Count} matching errors for search term: {SearchTerm}",
                results.Count, searchTerm);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching errors. Error: {Error}", ex.FullMessage());
            return [];
        }
    }
}