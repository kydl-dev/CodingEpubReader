using Application.Exceptions;
using Application.Interfaces;
using Application.UseCases.Cache.InvalidateCacheByPrefix;
using Application.UseCases.CoverImage.InvalidateBookCoverCache;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Books.DeleteBook;

public class DeleteBookCommandHandler(
    IBookRepository bookRepository,
    IReadingProgressRepository progressRepository,
    IBookmarkRepository bookmarkRepository,
    IHighlightRepository highlightRepository,
    IFileStorageService fileStorage,
    IMediator mediator) : IRequestHandler<DeleteBookCommand>
{
    public async Task Handle(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);
        var book = await bookRepository.GetByIdAsync(bookId, cancellationToken)
                   ?? throw new BookNotFoundException(request.BookId);

        // Delete all associated data first.
        await progressRepository.DeleteAsync(bookId, cancellationToken);

        // NOTE: Reading history is intentionally NOT deleted here.
        // The ReadingHistory entity stores a snapshot of the book's title/author/ISBN
        // and its BookId FK is set to NULL by the database (SetNull cascade) when
        // the Book row is deleted. This preserves reading stats in case the book is
        // re-imported in the future.

        var bookmarks = await bookmarkRepository.GetByBookIdAsync(bookId, cancellationToken);
        foreach (var bookmark in bookmarks)
            await bookmarkRepository.DeleteAsync(bookmark.Id, cancellationToken);

        var highlights = await highlightRepository.GetByBookIdAsync(bookId, cancellationToken);
        foreach (var highlight in highlights)
            await highlightRepository.DeleteAsync(highlight.Id, cancellationToken);

        // Remove the epub file from managed storage.
        if (fileStorage.FileExists(book.FilePath))
            await fileStorage.DeleteFromLibraryAsync(book.FilePath, cancellationToken);

        await bookRepository.DeleteAsync(bookId, cancellationToken);

        // Invalidate cover image cache for the deleted book
        await mediator.Send(new InvalidateBookCoverCacheCommand(request.BookId), cancellationToken);

        // Invalidate any cached data related to this book
        await mediator.Send(new InvalidateCacheByPrefixCommand($"book:{request.BookId}"), cancellationToken);
    }
}