using Application.DTOs;
using Application.Exceptions;
using AutoMapper;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Bookmark.AddBookmark;

public class AddBookmarkCommandHandler(
    IBookRepository bookRepository,
    IBookmarkRepository bookmarkRepository,
    IMapper mapper) : IRequestHandler<AddBookmarkCommand, BookmarkDto>
{
    public async Task<BookmarkDto> Handle(AddBookmarkCommand request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);

        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(request.BookId);

        var position = new ReadingPosition(bookId, request.ChapterId, request.Progress);
        var bookmark = Domain.Entities.Bookmark.Create(bookId, position, request.Type, request.Note);

        var saved = await bookmarkRepository.AddAsync(bookmark, cancellationToken);
        return mapper.Map<BookmarkDto>(saved);
    }
}