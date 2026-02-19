namespace Domain.Enums;

public enum BookmarkType
{
    /// <summary>A simple position marker with no associated text selection.</summary>
    Position,

    /// <summary>A bookmark tied to a highlighted or selected text range.</summary>
    Highlight,

    /// <summary>A user-defined bookmark with an optional note.</summary>
    Annotation,

    /// <summary>The last reading position, automatically saved.</summary>
    LastRead
}