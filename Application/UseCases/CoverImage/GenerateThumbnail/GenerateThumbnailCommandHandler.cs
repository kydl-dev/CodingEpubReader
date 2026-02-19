using MediatR;
using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;

namespace Application.UseCases.CoverImage.GenerateThumbnail;

/// <summary>
///     Handler for GenerateThumbnailCommand
/// </summary>
public sealed class GenerateThumbnailCommandHandler(
    ICoverImageCacheWorker coverImageCacheWorker,
    ILogger<GenerateThumbnailCommandHandler> logger)
    : IRequestHandler<GenerateThumbnailCommand, Unit>
{
    private readonly ICoverImageCacheWorker _coverImageCacheWorker =
        coverImageCacheWorker ?? throw new ArgumentNullException(nameof(coverImageCacheWorker));

    private readonly ILogger<GenerateThumbnailCommandHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Unit> Handle(GenerateThumbnailCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating thumbnail for book {BookId} at size {Width}x{Height}",
            request.BookId, request.Width, request.Height);

        await _coverImageCacheWorker.GenerateThumbnailAsync(
            request.BookId,
            request.SourcePath,
            request.Width,
            request.Height,
            cancellationToken);

        _logger.LogInformation("Thumbnail generated successfully for book {BookId}", request.BookId);
        return Unit.Value;
    }
}