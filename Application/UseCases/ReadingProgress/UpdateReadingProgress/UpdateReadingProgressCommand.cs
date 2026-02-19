using MediatR;

namespace Application.UseCases.ReadingProgress.UpdateReadingProgress;

/// <summary>Saves the current scroll/page position for a book chapter.</summary>
public sealed record UpdateReadingProgressCommand(
    Guid BookId,
    string ChapterId,
    double Progress) : IRequest;