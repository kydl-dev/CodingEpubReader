using Domain.Entities;

namespace Domain.Services;

/// <summary>
///     Calculates overall book reading progress and estimated time remaining
///     based on the current reading position.
/// </summary>
public abstract class ReadingProgressCalculator
{
    /// <summary>
    ///     Calculates the overall book-level progress as a value between 0.0 and 1.0.
    /// </summary>
    public static double CalculateOverallProgress(Book book, ReadingPosition position)
    {
        ArgumentNullException.ThrowIfNull(book);
        ArgumentNullException.ThrowIfNull(position);

        if (book.Chapters.Count == 0) return 0.0;

        var chapterIndex = book.GetChapterIndex(position.ChapterId);
        if (chapterIndex < 0) return 0.0;

        var chapterWeight = 1.0 / book.Chapters.Count;
        var completedChapters = chapterIndex * chapterWeight;
        var currentChapterProgress = position.Progress * chapterWeight;

        return Math.Clamp(completedChapters + currentChapterProgress, 0.0, 1.0);
    }
}