using Application.Interfaces;
using MediatR;

namespace Application.UseCases.Books.ExportCompleteBook;

/// <summary>
///     Handler for exporting a complete book as a single HTML document.
/// </summary>
public class ExportCompleteBookQueryHandler(IBookContentService contentService)
    : IRequestHandler<ExportCompleteBookQuery, string>
{
    private readonly IBookContentService _contentService =
        contentService ?? throw new ArgumentNullException(nameof(contentService));

    public async Task<string> Handle(
        ExportCompleteBookQuery request,
        CancellationToken cancellationToken)
    {
        return await _contentService.GetCompleteBookContentAsync(
            request.BookId,
            cancellationToken);
    }
}