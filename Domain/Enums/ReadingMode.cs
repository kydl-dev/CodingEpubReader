namespace Domain.Enums;

public enum ReadingMode
{
    /// <summary>Content is split into discrete pages; the user flips between them.</summary>
    Paginated,

    /// <summary>Content flows continuously; the user scrolls through it.</summary>
    Scrolled
}