using Application.DTOs.Book;
using Domain.ValueObjects;

namespace Application.Interfaces;

/// <summary>
///     Service for processing and preparing book content for display.
///     This interface is defined in Application and implemented in Infrastructure.
/// </summary>
public interface IBookContentService
{
    /// <summary>
    ///     Gets processed chapter content ready for display with CSS styling.
    /// </summary>
    /// <param name="bookId">The book identifier</param>
    /// <param name="chapterId">The chapter identifier</param>
    /// <param name="customStyle">Optional custom CSS style to apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete HTML document with styling</returns>
    Task<string> GetChapterContentAsync(
        Guid bookId,
        string chapterId,
        CssStyle? customStyle = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets the complete book content as a single HTML document.
    /// </summary>
    /// <param name="bookId">The book identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Complete HTML document containing all chapters</returns>
    Task<string> GetCompleteBookContentAsync(
        Guid bookId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Extracts plain text from a chapter (without HTML markup).
    /// </summary>
    /// <param name="bookId">The book identifier</param>
    /// <param name="chapterId">The chapter identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Plain text content of the chapter</returns>
    Task<string> GetChapterPlainTextAsync(
        Guid bookId,
        string chapterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets statistics about a book (cached).
    /// </summary>
    /// <param name="bookId">The book identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Book statistics including word count, chapters, reading time</returns>
    Task<BookStatisticsDto> GetBookStatisticsAsync(
        Guid bookId,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cleans up and sanitizes HTML content.
    /// </summary>
    /// <param name="html">Raw HTML content</param>
    /// <returns>Sanitized HTML</returns>
    string SanitizeHtml(string html);
}