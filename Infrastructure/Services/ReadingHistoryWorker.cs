using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;
using Shared.Exceptions;

namespace Infrastructure.Services;

/// <summary>
///     Service for managing reading history and progress.
///     Reading history is intentionally preserved even after a book is deleted —
///     the book's title, author and ISBN are snapshotted into the history record
///     at creation time for this purpose.
/// </summary>
public class ReadingHistoryWorker(
    IReadingHistoryRepository readingHistoryRepository,
    IReadingProgressRepository readingProgressRepository,
    IBookRepository bookRepository,
    ILogger<ReadingHistoryWorker> logger)
    : IReadingHistoryWorker
{
    private readonly IBookRepository _bookRepository =
        bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));

    private readonly ILogger<ReadingHistoryWorker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IReadingHistoryRepository _readingHistoryRepository =
        readingHistoryRepository ?? throw new ArgumentNullException(nameof(readingHistoryRepository));

    private readonly IReadingProgressRepository _readingProgressRepository =
        readingProgressRepository ?? throw new ArgumentNullException(nameof(readingProgressRepository));

    public async Task UpdateReadingTimeAsync(Guid bookId, TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bookIdValue = BookId.From(bookId);
            var history = await _readingHistoryRepository.GetByBookIdAsync(bookIdValue, cancellationToken);

            if (history == null)
            {
                // Snapshot the book's metadata so the history survives book deletion.
                var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);
                if (book == null)
                {
                    _logger.LogWarning("Cannot create reading history for unknown book {BookId}", bookId);
                    return;
                }

                history = ReadingHistory.Create(
                    bookIdValue,
                    book.Title,
                    book.PrimaryAuthor,
                    book.Metadata.Isbn);

                history.RecordSession(duration);
                await _readingHistoryRepository.AddAsync(history, cancellationToken);
                _logger.LogDebug("Created new reading history for book {BookId} ({Title})", bookId, book.Title);
            }
            else
            {
                history.RecordSession(duration);
                await _readingHistoryRepository.UpdateAsync(history, cancellationToken);
                _logger.LogDebug("Updated reading time for book {BookId}, total: {TotalTime}",
                    bookId, history.TotalReadingTime);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reading time for book {BookId}. Error: {Error}",
                bookId, ex.FullMessage());
            throw;
        }
    }

    public async Task UpdateProgressAsync(Guid bookId, string chapterId, int position,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bookIdValue = BookId.From(bookId);
            var normalizedProgress = position <= 1
                ? Math.Clamp(position, 0, 1) * 1.0
                : Math.Clamp(position / 100.0, 0.0, 1.0);

            var progress = await _readingProgressRepository.GetLastPositionAsync(bookIdValue, cancellationToken);

            if (progress == null)
            {
                var newProgress = new ReadingPosition(bookIdValue, chapterId, normalizedProgress);
                await _readingProgressRepository.SavePositionAsync(newProgress, cancellationToken);
                _logger.LogDebug("Created new reading progress for book {BookId}", bookId);
            }
            else
            {
                var updatedProgress = progress.WithChapter(chapterId).WithProgress(normalizedProgress);
                await _readingProgressRepository.SavePositionAsync(updatedProgress, cancellationToken);
                _logger.LogDebug("Updated reading progress for book {BookId}: Chapter={Chapter}, Position={Position}",
                    bookId, chapterId, normalizedProgress);
            }

            // Also update the last-read timestamp in reading history.
            var history = await _readingHistoryRepository.GetByBookIdAsync(bookIdValue, cancellationToken);
            if (history != null)
            {
                history.UpdateLastRead();
                await _readingHistoryRepository.UpdateAsync(history, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reading progress for book {BookId}. Error: {Error}",
                bookId, ex.FullMessage());
            throw;
        }
    }
}