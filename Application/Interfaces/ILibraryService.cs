using Application.DTOs.Book;

namespace Application.Interfaces;

/// <summary>
///     High-level service for library management operations that span multiple
///     domain repositories or require coordination beyond a single use case handler.
/// </summary>
public interface ILibraryService
{
    /// <summary>Returns all books in the library sorted by last opened date, then title.</summary>
    Task<IEnumerable<BookSummaryDto>> GetLibraryAsync(CancellationToken cancellationToken = default);

    /// <summary>Removes a book and all associated reading progress, bookmarks, and highlights.</summary>
    Task RemoveBookCompletelyAsync(Guid bookId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Scans <paramref name="folderPath" /> for epub files and imports any that are
    ///     not already in the library. Returns the number of newly imported books.
    /// </summary>
    Task<int> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default);
}