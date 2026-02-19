using Domain.ValueObjects;
using Shared.Exceptions;

namespace Domain.Entities;

public class ReadingPosition
{
    private ReadingPosition()
    {
    }

    public ReadingPosition(BookId bookId, string chapterId, double progress)
    {
        if (string.IsNullOrWhiteSpace(chapterId))
            throw new ArgumentException("ChapterId cannot be empty.", nameof(chapterId));
        if (progress is < 0.0 or > 1.0)
            throw new DomainException($"Progress must be between 0 and 1, but was {progress}.");

        BookId = bookId ?? throw new ArgumentNullException(nameof(bookId));
        ChapterId = chapterId;
        Progress = progress;
        SavedAt = DateTime.UtcNow;
    }

    public BookId BookId { get; } = null!;
    public string ChapterId { get; } = string.Empty;

    /// <summary>
    ///     A value between 0.0 and 1.0 representing how far into the chapter the reader is.
    /// </summary>
    public double Progress { get; }

    public DateTime SavedAt { get; private set; }

    public bool IsAtStart => Progress == 0.0;
    public bool IsAtEnd => Progress >= 1.0;

    public ReadingPosition WithProgress(double newProgress)
    {
        return new ReadingPosition(BookId, ChapterId, newProgress);
    }

    public ReadingPosition WithChapter(string newChapterId)
    {
        return new ReadingPosition(BookId, newChapterId, 0.0);
    }
}