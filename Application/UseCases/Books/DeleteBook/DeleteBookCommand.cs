using MediatR;

namespace Application.UseCases.Books.DeleteBook;

/// <summary>
///     Removes a book and all associated data (progress, bookmarks, highlights)
///     from the library. Also deletes the managed copy of the epub file.
/// </summary>
public record DeleteBookCommand(Guid BookId) : IRequest;