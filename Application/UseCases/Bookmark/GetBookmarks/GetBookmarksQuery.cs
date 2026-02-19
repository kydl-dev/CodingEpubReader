using Application.DTOs;
using MediatR;

namespace Application.UseCases.Bookmark.GetBookmarks;

public abstract record GetBookmarksQuery(Guid BookId) : IRequest<IEnumerable<BookmarkDto>>;