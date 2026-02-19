using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Cache.InvalidateCacheByPrefix;

/// <summary>
///     Handler for InvalidateCacheByPrefixCommand
/// </summary>
public sealed class InvalidateCacheByPrefixCommandHandler(
    ICacheService cacheService,
    ILogger<InvalidateCacheByPrefixCommandHandler> logger)
    : IRequestHandler<InvalidateCacheByPrefixCommand, Unit>
{
    private readonly ICacheService
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

    private readonly ILogger<InvalidateCacheByPrefixCommandHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public Task<Unit> Handle(InvalidateCacheByPrefixCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidating cache entries with prefix: {Prefix}", request.Prefix);

        _cacheService.RemoveByPrefix(request.Prefix);

        _logger.LogInformation("Cache entries with prefix {Prefix} invalidated successfully", request.Prefix);
        return Task.FromResult(Unit.Value);
    }
}