using Application.DTOs.Book;
using MediatR;

namespace Application.UseCases.Books.GetBookDetails;

public record GetBookDetailsQuery(Guid BookId) : IRequest<BookDto>;