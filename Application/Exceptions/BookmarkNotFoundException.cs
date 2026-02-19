using Shared.Exceptions;

namespace Application.Exceptions;

public class BookmarkNotFoundException(Guid bookmarkId)
    : DomainException($"Bookmark with Id '{bookmarkId}' was not found.")
{
    public Guid BookmarkId { get; } = bookmarkId;
}