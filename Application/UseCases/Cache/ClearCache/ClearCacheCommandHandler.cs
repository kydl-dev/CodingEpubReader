using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Cache.ClearCache;

/// <summary>
///     Handler for ClearCacheCommand
/// </summary>
public sealed class ClearCacheCommandHandler(
    ICacheService cacheService,
    ILogger<ClearCacheCommandHandler> logger)
    : IRequestHandler<ClearCacheCommand, Unit>
{
    private readonly ICacheService
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

    private readonly ILogger<ClearCacheCommandHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<Unit> Handle(ClearCacheCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Clearing all cached data");

        _cacheService.Clear();

        _logger.LogInformation("Cache cleared successfully");
        return Task.FromResult(Unit.Value);
    }
}