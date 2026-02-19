using Application.DTOs;
using Application.Exceptions;
using AutoMapper;
using Domain.Repositories;
using Domain.Services;
using Domain.ValueObjects;
using MediatR;

namespace Application.UseCases.Highlight.AddHighlight;

public class AddHighlightCommandHandler(
    IBookRepository bookRepository,
    IHighlightRepository highlightRepository,
    IMapper mapper) : IRequestHandler<AddHighlightCommand, HighlightDto>
{
    public async Task<HighlightDto> Handle(AddHighlightCommand request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);

        if (!await bookRepository.ExistsAsync(bookId, cancellationToken))
            throw new BookNotFoundException(request.BookId);

        // Delegate selection validation to the domain service.
        var textRange = TextSelectionService.CreateSelection(
            request.ChapterId,
            request.StartOffset,
            request.EndOffset,
            request.SelectedText);

        var highlight = Domain.Entities.Highlight.Create(bookId, textRange, request.Color, request.Note);

        var saved = await highlightRepository.AddAsync(highlight, cancellationToken);
        return mapper.Map<HighlightDto>(saved);
    }
}