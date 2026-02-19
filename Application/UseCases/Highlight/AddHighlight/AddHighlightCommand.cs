using Application.DTOs;
using MediatR;

namespace Application.UseCases.Highlight.AddHighlight;

public abstract record AddHighlightCommand(
    Guid BookId,
    string ChapterId,
    int StartOffset,
    int EndOffset,
    string SelectedText,
    string Color = "#FFFF00",
    string? Note = null) : IRequest<HighlightDto>;