using Application.DTOs.Book;
using MediatR;

namespace Application.UseCases.Books.GetAllBooks;

/// <summary>Returns a lightweight summary of every book in the library.</summary>
public record GetAllBooksQuery : IRequest<IEnumerable<BookSummaryDto>>;