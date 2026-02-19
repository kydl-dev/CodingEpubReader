using Application.Interfaces;
using MediatR;

namespace Application.UseCases.Books.GetChapterPlainText;

/// <summary>
///     Handler for extracting plain text from a chapter.
/// </summary>
public class GetChapterPlainTextQueryHandler(IBookContentService contentService)
    : IRequestHandler<GetChapterPlainTextQuery, string>
{
    private readonly IBookContentService _contentService =
        contentService ?? throw new ArgumentNullException(nameof(contentService));

    public async Task<string> Handle(
        GetChapterPlainTextQuery request,
        CancellationToken cancellationToken)
    {
        return await _contentService.GetChapterPlainTextAsync(
            request.BookId,
            request.ChapterId,
            cancellationToken);
    }
}