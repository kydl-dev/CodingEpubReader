using Shared.Exceptions;

namespace Application.Exceptions;

public class BookNotFoundException(Guid bookId) : DomainException($"Book with Id '{bookId}' was not found.")
{
    public Guid BookId { get; } = bookId;
}