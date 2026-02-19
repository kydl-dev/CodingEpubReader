using MediatR;

namespace Application.UseCases.CoverImage.GenerateThumbnail;

/// <summary>
///     Command to generate a thumbnail for a book cover
/// </summary>
public sealed record GenerateThumbnailCommand(
    Guid BookId,
    string SourcePath,
    int Width,
    int Height) : IRequest<Unit>;