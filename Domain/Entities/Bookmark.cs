using Domain.Enums;
using Domain.ValueObjects;
using Shared.Exceptions;

namespace Domain.Entities;

public class Bookmark
{
    private Bookmark()
    {
    }

    private Bookmark(Guid id, BookId bookId, ReadingPosition position, BookmarkType type, string? note)
    {
        Id = id;
        BookId = bookId;
        Position = position;
        Type = type;
        Note = note;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public BookId BookId { get; private set; } = null!;
    public ReadingPosition Position { get; private set; } = null!;
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public BookmarkType Type { get; private set; }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public static Bookmark Create(BookId bookId, ReadingPosition position, BookmarkType type, string? note = null)
    {
        if (bookId is null)
            throw new ArgumentNullException(nameof(bookId));
        if (position is null)
            throw new ArgumentNullException(nameof(position));
        return !bookId.Equals(position.BookId)
            ? throw new InvalidBookmarkException("Bookmark BookId must match the ReadingPosition BookId.")
            : new Bookmark(Guid.NewGuid(), bookId, position, type, note);
    }

    public void UpdateNote(string? note)
    {
        Note = note;
    }
}