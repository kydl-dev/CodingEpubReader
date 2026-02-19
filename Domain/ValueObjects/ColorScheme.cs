namespace Domain.ValueObjects;

/// <summary>
///     Represents a color scheme for book content rendering.
/// </summary>
public sealed record ColorScheme
{
    // Private parameterless constructor for EF Core
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    public ColorScheme()
    {
        // EF Core will set properties directly
    }
#pragma warning restore CS8618

    public ColorScheme(
        string Background,
        string Text,
        string Link,
        string Selection,
        string Surface,
        string Border)
    {
        this.Background = Background;
        this.Text = Text;
        this.Link = Link;
        this.Selection = Selection;
        this.Surface = Surface;
        this.Border = Border;
    }

    public string Background { get; init; }
    public string Text { get; init; }
    public string Link { get; init; }
    public string Selection { get; init; }
    public string Surface { get; init; }
    public string Border { get; init; }
}