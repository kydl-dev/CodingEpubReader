using MediatR;

namespace Application.UseCases.Books.GetChapterPlainText;

/// <summary>
///     Query to retrieve plain text content of a chapter without HTML markup.
///     Useful for copying text, text-to-speech, translation, or analysis.
/// </summary>
public record GetChapterPlainTextQuery(
    Guid BookId,
    string ChapterId) : IRequest<string>;