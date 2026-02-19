using Domain.ValueObjects;

namespace Domain.Entities;

public class Highlight
{
    private Highlight()
    {
    }

    private Highlight(Guid id, BookId bookId, TextRange textRange, string color, string? note)
    {
        Id = id;
        BookId = bookId;
        TextRange = textRange;
        Color = color;
        Note = note;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public BookId BookId { get; private set; } = null!;
    public TextRange TextRange { get; private set; } = null!;

    /// <summary>
    ///     The highlight color stored as a hex string, e.g. "#FFFF00".
    /// </summary>
    public string Color { get; private set; } = string.Empty;

    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public static Highlight Create(BookId bookId, TextRange textRange, string color = "#FFFF00", string? note = null)
    {
        if (bookId is null)
            throw new ArgumentNullException(nameof(bookId));
        if (textRange is null)
            throw new ArgumentNullException(nameof(textRange));
        if (textRange.IsEmpty)
            throw new ArgumentException("Cannot create a highlight over an empty text range.", nameof(textRange));
        return string.IsNullOrWhiteSpace(color)
            ? throw new ArgumentException("Highlight color cannot be empty.", nameof(color))
            : new Highlight(Guid.NewGuid(), bookId, textRange, color, note);
    }

    public void UpdateNote(string? note)
    {
        Note = note;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color cannot be empty.", nameof(color));

        Color = color;
        UpdatedAt = DateTime.UtcNow;
    }
}