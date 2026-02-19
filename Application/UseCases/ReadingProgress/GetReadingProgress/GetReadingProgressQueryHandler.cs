using Application.DTOs;
using Application.Exceptions;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.ReadingProgress.GetReadingProgress;

public class GetReadingProgressQueryHandler(
    IBookRepository bookRepository,
    IReadingProgressRepository progressRepository,
    IMapper mapper) : IRequestHandler<GetReadingProgressQuery, ReadingPositionDto?>
{
    public async Task<ReadingPositionDto?> Handle(
        GetReadingProgressQuery request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);

        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(request.BookId);

        var position = await progressRepository.GetLastPositionAsync(bookId, cancellationToken);

        // Return null (no DTO) when the user has never opened this book.
        return position is null ? null : mapper.Map<ReadingPositionDto>(position);
    }
}