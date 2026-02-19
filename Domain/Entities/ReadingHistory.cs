using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
///     Tracks the reading history and accumulated time spent reading a book.
///     Intentionally survives book deletion — BookId becomes null when its book is
///     removed, but BookTitle / BookAuthor / BookIsbn are stored as a snapshot so the
///     history remains meaningful. This lets you restore a book in the future and still
///     have its full reading stats.
/// </summary>
public class ReadingHistory
{
    // EF Core / serialization constructor
    private ReadingHistory()
    {
    }

    private ReadingHistory(
        Guid id,
        BookId bookId,
        string bookTitle,
        string bookAuthor,
        string? bookIsbn,
        DateTime lastReadAt,
        TimeSpan totalReadingTime,
        int totalSessions)
    {
        Id = id;
        BookId = bookId ?? throw new ArgumentNullException(nameof(bookId));
        BookTitle = bookTitle ?? throw new ArgumentNullException(nameof(bookTitle));
        BookAuthor = bookAuthor ?? throw new ArgumentNullException(nameof(bookAuthor));
        BookIsbn = bookIsbn;
        LastReadAt = lastReadAt;
        TotalReadingTime = totalReadingTime;
        TotalSessions = totalSessions;
    }

    public Guid Id { get; private set; }

    /// <summary>
    ///     Reference to the book. Becomes null when the book is deleted — the snapshot
    ///     fields below keep the history meaningful even without this FK.
    /// </summary>
    public BookId? BookId { get; private set; }

    /// <summary>Navigation property — null when the book has been deleted.</summary>
    public Book? Book { get; private set; }

    // ── Book metadata snapshot ────────────────────────────────────────────────
    // Stored at creation time so the history record is self-contained after deletion.

    /// <summary>Title of the book at the time it was first read.</summary>
    public string BookTitle { get; private set; } = string.Empty;

    /// <summary>Primary author of the book at the time it was first read.</summary>
    public string BookAuthor { get; private set; } = string.Empty;

    /// <summary>ISBN of the book, if available. Used for re-linking if the book is re-imported.</summary>
    public string? BookIsbn { get; private set; }

    // ── Reading stats ─────────────────────────────────────────────────────────

    /// <summary>The last time this book was read.</summary>
    public DateTime LastReadAt { get; private set; }

    /// <summary>The accumulated total time spent reading this book.</summary>
    public TimeSpan TotalReadingTime { get; private set; }

    /// <summary>The total number of reading sessions for this book.</summary>
    public int TotalSessions { get; private set; }

    /// <summary>Whether this history entry is still linked to an existing book.</summary>
    public bool IsBookPresent => BookId is not null;

    /// <summary>Average duration per reading session.</summary>
    public TimeSpan AverageSessionDuration =>
        TotalSessions > 0
            ? TimeSpan.FromTicks(TotalReadingTime.Ticks / TotalSessions)
            : TimeSpan.Zero;

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ReadingHistory Create(BookId bookId, string bookTitle, string bookAuthor, string? bookIsbn = null)
    {
        ArgumentNullException.ThrowIfNull(bookId);
        if (string.IsNullOrWhiteSpace(bookTitle))
            throw new ArgumentException("Book title cannot be empty.", nameof(bookTitle));
        if (string.IsNullOrWhiteSpace(bookAuthor))
            throw new ArgumentException("Book author cannot be empty.", nameof(bookAuthor));

        return new ReadingHistory(
            Guid.NewGuid(),
            bookId,
            bookTitle,
            bookAuthor,
            bookIsbn,
            DateTime.UtcNow,
            TimeSpan.Zero,
            0);
    }

    // ── Behaviour ─────────────────────────────────────────────────────────────

    /// <summary>Records a new reading session.</summary>
    public void RecordSession(TimeSpan sessionDuration)
    {
        if (sessionDuration < TimeSpan.Zero)
            throw new ArgumentException("Session duration cannot be negative.", nameof(sessionDuration));

        LastReadAt = DateTime.UtcNow;
        TotalReadingTime += sessionDuration;
        TotalSessions++;
    }

    /// <summary>
    ///     Updates the last-read timestamp without incrementing the session count.
    ///     Useful for auto-save scenarios.
    /// </summary>
    public void UpdateLastRead()
    {
        LastReadAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Called when the linked book is being deleted. Clears the FK so the record
    ///     is not removed by cascade, while keeping all stats and the metadata snapshot.
    /// </summary>
    public void DetachBook()
    {
        BookId = null;
        Book = null;
    }
}