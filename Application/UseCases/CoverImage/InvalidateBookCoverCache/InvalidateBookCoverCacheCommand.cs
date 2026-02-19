using MediatR;

namespace Application.UseCases.CoverImage.InvalidateBookCoverCache;

/// <summary>
///     Command to invalidate cached cover images for a specific book
/// </summary>
public sealed record InvalidateBookCoverCacheCommand(Guid BookId) : IRequest<Unit>;