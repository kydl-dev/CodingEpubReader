using Application.DTOs;
using MediatR;

namespace Application.UseCases.Highlight.GetHighlights;

/// <summary>
///     Returns highlights for a book. Optionally filters to a single chapter
///     when <see cref="ChapterId" /> is provided.
/// </summary>
public abstract record GetHighlightsQuery(Guid BookId, string? ChapterId = null) : IRequest<IEnumerable<HighlightDto>>;