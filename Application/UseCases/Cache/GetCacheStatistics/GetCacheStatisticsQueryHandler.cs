using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Cache.GetCacheStatistics;

/// <summary>
///     Handler for GetCacheStatisticsQuery
/// </summary>
public sealed class GetCacheStatisticsQueryHandler(
    ICacheService cacheService,
    ILogger<GetCacheStatisticsQueryHandler> logger)
    : IRequestHandler<GetCacheStatisticsQuery, CacheStatisticsDto>
{
    private readonly ICacheService
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

    private readonly ILogger<GetCacheStatisticsQueryHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<CacheStatisticsDto> Handle(GetCacheStatisticsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving cache statistics");

        var allKeys = _cacheService.GetAllKeys();
        var totalCount = _cacheService.GetCachedItemsCount();

        // Group keys by prefix (first part before ':')
        var keysByPrefix = allKeys
            .Select(key => key.Contains(':') ? key.Split(':')[0] : "other")
            .GroupBy(prefix => prefix)
            .ToDictionary(g => g.Key, g => g.Count());

        var statistics = new CacheStatisticsDto
        {
            TotalCachedItems = totalCount,
            AllKeys = allKeys,
            KeysByPrefix = keysByPrefix
        };

        _logger.LogDebug("Cache statistics retrieved: {Count} total items, {Prefixes} unique prefixes",
            totalCount, keysByPrefix.Count);

        return Task.FromResult(statistics);
    }
}