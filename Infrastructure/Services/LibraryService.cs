using Application.DTOs.Book;
using Application.Interfaces;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Infrastructure.Services;

public class LibraryService(
    IBookRepository bookRepository,
    IBookmarkRepository bookmarkRepository,
    IHighlightRepository highlightRepository,
    IReadingProgressRepository readingProgressRepository,
    IFileStorageService fileStorageService,
    IEpubParser epubParser,
    IMapper mapper,
    ILogger<LibraryService> logger)
    : ILibraryService
{
    private readonly IBookmarkRepository _bookmarkRepository =
        bookmarkRepository ?? throw new ArgumentNullException(nameof(bookmarkRepository));

    private readonly IBookRepository _bookRepository =
        bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));

    private readonly IEpubParser _epubParser = epubParser ?? throw new ArgumentNullException(nameof(epubParser));

    private readonly IFileStorageService _fileStorageService =
        fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));

    private readonly IHighlightRepository _highlightRepository =
        highlightRepository ?? throw new ArgumentNullException(nameof(highlightRepository));

    private readonly ILogger<LibraryService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    private readonly IReadingProgressRepository _readingProgressRepository =
        readingProgressRepository ?? throw new ArgumentNullException(nameof(readingProgressRepository));

    public async Task<IEnumerable<BookSummaryDto>> GetLibraryAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching library books");
        var books = await _bookRepository.GetAllAsync(cancellationToken);
        var bookSummaryDtos = _mapper.Map<IEnumerable<BookSummaryDto>>(books).ToList();
        _logger.LogInformation("Found {BookCount} books in library", bookSummaryDtos.Count);
        return bookSummaryDtos;
    }

    public async Task RemoveBookCompletelyAsync(Guid bookId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Removing book completely: {BookId}", bookId);

        var bookIdValue = BookId.From(bookId);
        var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);

        if (book == null)
        {
            _logger.LogWarning("Book not found: {BookId}", bookId);
            return;
        }

        try
        {
            _logger.LogDebug("Deleting bookmarks for book: {BookId}", bookId);
            var bookmarks = await _bookmarkRepository.GetByBookIdAsync(bookIdValue, cancellationToken);
            foreach (var bookmark in bookmarks)
                await _bookmarkRepository.DeleteAsync(bookmark.Id, cancellationToken);

            _logger.LogDebug("Deleting highlights for book: {BookId}", bookId);
            var highlights = await _highlightRepository.GetByBookIdAsync(bookIdValue, cancellationToken);
            foreach (var highlight in highlights)
                await _highlightRepository.DeleteAsync(highlight.Id, cancellationToken);

            _logger.LogDebug("Deleting reading progress for book: {BookId}", bookId);
            await _readingProgressRepository.DeleteAsync(bookIdValue, cancellationToken);

            // NOTE: Reading history is intentionally NOT deleted here.
            // The ReadingHistory entity stores a snapshot of the book title/author/ISBN
            // and its BookId FK is set to NULL by the database (SetNull cascade) when
            // the Book row is deleted. This preserves reading stats for future re-imports.

            if (_fileStorageService.FileExists(book.FilePath))
            {
                _logger.LogDebug("Deleting book file: {FilePath}", book.FilePath);
                await _fileStorageService.DeleteFromLibraryAsync(book.FilePath, cancellationToken);
            }

            _logger.LogDebug("Deleting book record: {BookId}", bookId);
            await _bookRepository.DeleteAsync(bookIdValue, cancellationToken);

            _logger.LogInformation("Successfully removed book completely: {BookId}", bookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove book completely: {BookId}. Error: {Error}", bookId,
                ex.FullMessage());
            throw;
        }
    }

    public async Task<int> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new ArgumentException("Folder path cannot be empty.", nameof(folderPath));

        _logger.LogInformation("Importing books from folder: {FolderPath}", folderPath);

        var epubFiles = _fileStorageService.GetEpubFilesInFolder(folderPath);
        var importedCount = 0;
        var skippedCount = 0;

        var existingBooks = (await _bookRepository.GetAllAsync(cancellationToken)).ToList();

        foreach (var filePath in epubFiles)
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug("Processing file: {FilePath}", filePath);

                var parsedBook = await _epubParser.ParseAsync(filePath, cancellationToken);

                var isDuplicate = existingBooks.Any(existingBook =>
                {
                    if (!string.IsNullOrWhiteSpace(parsedBook.Metadata.Isbn) &&
                        !string.IsNullOrWhiteSpace(existingBook.Metadata.Isbn) &&
                        parsedBook.Metadata.Isbn.Equals(existingBook.Metadata.Isbn, StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (!string.IsNullOrWhiteSpace(parsedBook.Metadata.Uuid) &&
                        !string.IsNullOrWhiteSpace(existingBook.Metadata.Uuid) &&
                        parsedBook.Metadata.Uuid.Equals(existingBook.Metadata.Uuid, StringComparison.OrdinalIgnoreCase))
                        return true;

                    return parsedBook.Title.Equals(existingBook.Title, StringComparison.OrdinalIgnoreCase)
                           && parsedBook.PrimaryAuthor.Equals(existingBook.PrimaryAuthor,
                               StringComparison.OrdinalIgnoreCase);
                });

                if (isDuplicate)
                {
                    _logger.LogDebug("Book already in library (duplicate), skipping: {Title} by {Author}",
                        parsedBook.Title, parsedBook.PrimaryAuthor);
                    skippedCount++;
                    continue;
                }

                var libraryPath = await _fileStorageService.CopyToLibraryAsync(filePath, cancellationToken);
                var bookInLibrary = await _epubParser.ParseAsync(libraryPath, cancellationToken);
                await _bookRepository.AddAsync(bookInLibrary, cancellationToken);
                existingBooks.Add(bookInLibrary);

                importedCount++;
                _logger.LogInformation("Successfully imported book: {Title}", bookInLibrary.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to import book from file: {FilePath}. Error: {Error}", filePath,
                    ex.FullMessage());
            }

        _logger.LogInformation("Import complete: {ImportedCount} books imported, {SkippedCount} duplicates skipped",
            importedCount, skippedCount);

        return importedCount;
    }
}