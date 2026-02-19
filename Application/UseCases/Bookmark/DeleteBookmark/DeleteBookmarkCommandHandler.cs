using Application.Exceptions;
using Domain.Repositories;
using MediatR;

namespace Application.UseCases.Bookmark.DeleteBookmark;

public class DeleteBookmarkCommandHandler(
    IBookmarkRepository bookmarkRepository) : IRequestHandler<DeleteBookmarkCommand>
{
    public async Task Handle(DeleteBookmarkCommand request, CancellationToken cancellationToken)
    {
        var bookmark = await bookmarkRepository.GetByIdAsync(request.BookmarkId, cancellationToken)
                       ?? throw new BookmarkNotFoundException(request.BookmarkId);

        await bookmarkRepository.DeleteAsync(bookmark.Id, cancellationToken);
    }
}