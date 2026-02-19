using MediatR;

namespace Application.UseCases.Cache.GetCacheStatistics;

/// <summary>
///     Query to get cache statistics and diagnostics
/// </summary>
public sealed record GetCacheStatisticsQuery : IRequest<CacheStatisticsDto>;

/// <summary>
///     DTO representing cache statistics
/// </summary>
public sealed record CacheStatisticsDto
{
    public int TotalCachedItems { get; init; }
    public IReadOnlyCollection<string> AllKeys { get; init; } = [];
    public Dictionary<string, int> KeysByPrefix { get; init; } = new();
}