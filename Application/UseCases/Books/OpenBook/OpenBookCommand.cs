using Application.DTOs.Book;
using MediatR;

namespace Application.UseCases.Books.OpenBook;

/// <summary>
///     Opens a book for reading. Records the last opened timestamp and returns
///     the full book detail including chapters and TOC.
/// </summary>
public record OpenBookCommand(Guid BookId) : IRequest<BookDto>;