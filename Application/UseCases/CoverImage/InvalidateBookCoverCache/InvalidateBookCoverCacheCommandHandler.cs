using MediatR;
using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;

namespace Application.UseCases.CoverImage.InvalidateBookCoverCache;

/// <summary>
///     Handler for InvalidateBookCoverCacheCommand
/// </summary>
public sealed class InvalidateBookCoverCacheCommandHandler(
    ICoverImageCacheWorker coverImageCacheWorker,
    ILogger<InvalidateBookCoverCacheCommandHandler> logger)
    : IRequestHandler<InvalidateBookCoverCacheCommand, Unit>
{
    private readonly ICoverImageCacheWorker _coverImageCacheWorker =
        coverImageCacheWorker ?? throw new ArgumentNullException(nameof(coverImageCacheWorker));

    private readonly ILogger<InvalidateBookCoverCacheCommandHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Unit> Handle(InvalidateBookCoverCacheCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invalidating cover cache for book {BookId}", request.BookId);

        await _coverImageCacheWorker.InvalidateCacheAsync(request.BookId, cancellationToken);

        _logger.LogInformation("Cover cache invalidated successfully for book {BookId}", request.BookId);
        return Unit.Value;
    }
}