using Application.DTOs.Book;
using Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Books.GetBookStatistics;

/// <summary>
///     Retrieves cached statistics about a book including word count,
///     reading time estimates, and metadata.
/// </summary>
public class GetBookStatisticsQueryHandler(
    IBookContentService contentService,
    ILogger<GetBookStatisticsQueryHandler> logger)
    : IRequestHandler<GetBookStatisticsQuery, BookStatisticsDto>
{
    private readonly IBookContentService _contentService = contentService
                                                           ?? throw new ArgumentNullException(nameof(contentService));

    private readonly ILogger<GetBookStatisticsQueryHandler> _logger = logger
                                                                      ?? throw new ArgumentNullException(
                                                                          nameof(logger));

    public async Task<BookStatisticsDto> Handle(
        GetBookStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting statistics for book {BookId}",
            request.BookId);

        try
        {
            var statistics = await _contentService.GetBookStatisticsAsync(
                request.BookId,
                cancellationToken);

            _logger.LogInformation(
                "Retrieved statistics for book {BookId}: {Chapters} chapters, {Words} words, ~{Minutes} min read",
                request.BookId,
                statistics.TotalChapters,
                statistics.TotalWords,
                statistics.EstimatedReadingTimeMinutes);

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting statistics for book {BookId}",
                request.BookId);
            throw;
        }
    }
}