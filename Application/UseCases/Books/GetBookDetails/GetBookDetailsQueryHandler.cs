using Application.DTOs.Book;
using Application.Exceptions;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Books.GetBookDetails;

public class GetBookDetailsQueryHandler(
    IBookRepository bookRepository,
    IMapper mapper) : IRequestHandler<GetBookDetailsQuery, BookDto>
{
    public async Task<BookDto> Handle(GetBookDetailsQuery request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);
        var book = await bookRepository.GetByIdAsync(bookId, cancellationToken)
                   ?? throw new BookNotFoundException(request.BookId);

        return mapper.Map<BookDto>(book);
    }
}