using Shared.BackgroundWorkers.Utils;

namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Interface for log statistics service
/// </summary>
public interface ILogStatisticsService
{
    Task CacheStatisticsAsync(LogStatistics statistics, CancellationToken cancellationToken = default);
    Task<LogStatistics?> GetCachedStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ErrorMessageInfo>> SearchErrorsAsync(string searchTerm,
        CancellationToken cancellationToken = default);
}