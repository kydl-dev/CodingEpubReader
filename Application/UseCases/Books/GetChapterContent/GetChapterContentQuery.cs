using MediatR;

namespace Application.UseCases.Books.GetChapterContent;

/// <summary>
///     Query to retrieve processed chapter content with CSS styling for WebView display.
/// </summary>
public record GetChapterContentQuery(
    Guid BookId,
    string ChapterId,
    Guid? CustomStyleId = null) : IRequest<string>;