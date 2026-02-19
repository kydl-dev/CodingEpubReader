using Shared.Exceptions;

namespace Application.Exceptions;

public class ChapterNotFoundException(Guid bookId, string chapterId)
    : DomainException($"Chapter '{chapterId}' was not found in book '{bookId}'.")
{
    public Guid BookId { get; } = bookId;
    public string ChapterId { get; } = chapterId;
}