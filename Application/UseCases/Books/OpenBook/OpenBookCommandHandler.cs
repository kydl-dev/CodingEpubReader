using Application.DTOs.Book;
using Application.Exceptions;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Books.OpenBook;

public class OpenBookCommandHandler(
    IBookRepository bookRepository,
    IMapper mapper) : IRequestHandler<OpenBookCommand, BookDto>
{
    public async Task<BookDto> Handle(OpenBookCommand request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);
        var book = await bookRepository.GetByIdAsync(bookId, cancellationToken)
                   ?? throw new BookNotFoundException(request.BookId);

        book.MarkAsOpened();
        await bookRepository.UpdateAsync(book, cancellationToken);

        return mapper.Map<BookDto>(book);
    }
}