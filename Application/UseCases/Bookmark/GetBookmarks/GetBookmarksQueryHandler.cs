using Application.DTOs;
using Application.Exceptions;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Bookmark.GetBookmarks;

public class GetBookmarksQueryHandler(
    IBookRepository bookRepository,
    IBookmarkRepository bookmarkRepository,
    IMapper mapper) : IRequestHandler<GetBookmarksQuery, IEnumerable<BookmarkDto>>
{
    public async Task<IEnumerable<BookmarkDto>> Handle(
        GetBookmarksQuery request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);

        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(request.BookId);

        var bookmarks = await bookmarkRepository.GetByBookIdAsync(bookId, cancellationToken);
        return mapper.Map<IEnumerable<BookmarkDto>>(bookmarks);
    }
}