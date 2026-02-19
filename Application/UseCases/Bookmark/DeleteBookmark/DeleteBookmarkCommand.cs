using MediatR;

namespace Application.UseCases.Bookmark.DeleteBookmark;

public abstract record DeleteBookmarkCommand(Guid BookmarkId) : IRequest;