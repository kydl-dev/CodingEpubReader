using Domain.Enums;

namespace Domain.ValueObjects;

/// <summary>
///     Represents an Avalonia UI theme that controls the application interface appearance.
///     This is separate from CssStyle which controls EPUB book content rendering.
/// </summary>
public sealed record Theme
{
    public static readonly Theme Light = new(
        "Light",
        ThemeKind.Light);

    public static readonly Theme Dark = new(
        "Dark",
        ThemeKind.Dark);

    public static readonly Theme Sepia = new(
        "Sepia",
        ThemeKind.Sepia);

    private Theme(string name, ThemeKind kind)
    {
        Name = name;
        Kind = kind;
    }

    public string Name { get; }
    public ThemeKind Kind { get; }

    /// <summary>
    ///     Gets the Avalonia ThemeVariant name for this theme.
    /// </summary>
    public string AvaloniaThemeVariant => Kind switch
    {
        ThemeKind.Light => "Light",
        ThemeKind.Dark => "Dark",
        ThemeKind.Sepia => "Light", // Sepia uses light variant
        _ => "Dark"
    };

    public static Theme FromKind(ThemeKind kind)
    {
        return kind switch
        {
            ThemeKind.Light => Light,
            ThemeKind.Dark => Dark,
            ThemeKind.Sepia => Sepia,
            _ => Dark
        };
    }
}