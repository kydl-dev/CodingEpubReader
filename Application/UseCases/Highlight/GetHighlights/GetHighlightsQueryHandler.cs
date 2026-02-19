using Application.DTOs;
using Application.Exceptions;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Highlight.GetHighlights;

public class GetHighlightsQueryHandler(
    IBookRepository bookRepository,
    IHighlightRepository highlightRepository,
    IMapper mapper) : IRequestHandler<GetHighlightsQuery, IEnumerable<HighlightDto>>
{
    public async Task<IEnumerable<HighlightDto>> Handle(
        GetHighlightsQuery request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);

        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(request.BookId);

        var highlights = request.ChapterId is not null
            ? await highlightRepository.GetByChapterAsync(bookId, request.ChapterId, cancellationToken)
            : await highlightRepository.GetByBookIdAsync(bookId, cancellationToken);

        return mapper.Map<IEnumerable<HighlightDto>>(highlights);
    }
}