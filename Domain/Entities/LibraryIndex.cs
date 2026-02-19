using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
///     Represents an indexed entry in the library catalog.
///     This provides quick access to book metadata without loading full book data.
/// </summary>
public class LibraryIndex
{
    // EF Core / serialization constructor
    private LibraryIndex()
    {
    }

    private LibraryIndex(
        Guid id,
        BookId bookId,
        string title,
        string author,
        DateTime indexedAt,
        DateTime? lastAccessedAt)
    {
        Id = id;
        BookId = bookId;
        Title = title;
        Author = author;
        IndexedAt = indexedAt;
        LastAccessedAt = lastAccessedAt;
    }

    public Guid Id { get; private set; }

    /// <summary>
    ///     Reference to the Book entity
    /// </summary>
    public BookId BookId { get; private set; } = null!;

    /// <summary>
    ///     Navigation property to the Book
    /// </summary>
    public Book Book { get; private set; } = null!;

    /// <summary>
    ///     Book title (cached for quick search)
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    ///     Primary author (cached for quick search)
    /// </summary>
    public string Author { get; private set; } = string.Empty;

    /// <summary>
    ///     When this book was added to the index
    /// </summary>
    public DateTime IndexedAt { get; private set; }

    /// <summary>
    ///     When this book was last accessed
    /// </summary>
    public DateTime? LastAccessedAt { get; private set; }

    /// <summary>
    ///     Number of times this book has been opened
    /// </summary>
    public int AccessCount { get; private set; }

    /// <summary>
    ///     Optional tags for categorization
    /// </summary>
    public string? Tags { get; private set; }

    /// <summary>
    ///     Whether this book is marked as favorite
    /// </summary>
    public bool IsFavorite { get; private set; }

    public static LibraryIndex Create(BookId bookId, string title, string author)
    {
        if (bookId == null)
            throw new ArgumentNullException(nameof(bookId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author cannot be empty.", nameof(author));

        return new LibraryIndex(
            Guid.NewGuid(),
            bookId,
            title,
            author,
            DateTime.UtcNow,
            null);
    }

    /// <summary>
    ///     Records an access to this book
    /// </summary>
    public void RecordAccess()
    {
        LastAccessedAt = DateTime.UtcNow;
        AccessCount++;
    }

    /// <summary>
    ///     Updates the cached metadata
    /// </summary>
    public void UpdateMetadata(string title, string author)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author cannot be empty.", nameof(author));

        Title = title;
        Author = author;
    }

    /// <summary>
    ///     Sets or updates tags
    /// </summary>
    public void SetTags(string tags)
    {
        Tags = tags;
    }

    /// <summary>
    ///     Toggles favorite status
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
    }

    /// <summary>
    ///     Sets favorite status
    /// </summary>
    public void SetFavorite(bool isFavorite)
    {
        IsFavorite = isFavorite;
    }
}