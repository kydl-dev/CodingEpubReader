namespace Domain.ValueObjects;

/// <summary>
///     Represents styling for EPUB book content rendering in WebView.
///     Includes both typography (fonts, spacing) and color scheme.
///     This is separate from Theme which controls the Avalonia UI appearance.
/// </summary>
public sealed record CssStyle
{
    private const double MinFontSize = 8.0;
    private const double MaxFontSize = 48.0;

    private const string SyntaxHighlightDraculaColorfulCss = """
                                                             /* Dracula Colorful syntax highlight */
                                                             pre .code-kw, pre.source-code .code-kw, pre code .code-kw { color: #ff79c6; font-weight: 600; }
                                                             pre .code-str, pre.source-code .code-str, pre code .code-str { color: #f1fa8c; }
                                                             pre .code-num, pre.source-code .code-num, pre code .code-num { color: #FFB86C; }
                                                             pre .code-com, pre.source-code .code-com, pre code .code-com { color: #6272a4; font-style: italic; }
                                                             pre .code-fn, pre.source-code .code-fn, pre code .code-fn { color: #50fa7b; }
                                                             pre .code-method, pre.source-code .code-method, pre code .code-method { color: #69FF94; }
                                                             pre .code-cls, pre.source-code .code-cls, pre code .code-cls { color: #8be9fd; font-weight: 600; }
                                                             pre .code-dec, pre.source-code .code-dec, pre code .code-dec { color: #fff36c; }
                                                             pre .code-builtin, pre.source-code .code-builtin, pre code .code-builtin { color: #BD93F9; }
                                                             pre .code-op, pre.source-code .code-op, pre code .code-op { color: #FF92DF; }
                                                             """;

    private const string SyntaxHighlightDraculaAlucardCss = """
                                                            /* Dracula Alucard syntax highlight */
                                                            pre .code-kw, pre.source-code .code-kw, pre code .code-kw { color: #A3144D; font-weight: 600; }
                                                            pre .code-str, pre.source-code .code-str, pre code .code-str { color: #846E15; }
                                                            pre .code-num, pre.source-code .code-num, pre code .code-num { color: #A34D14; }
                                                            pre .code-com, pre.source-code .code-com, pre code .code-com { color: #6C664B; font-style: italic; }
                                                            pre .code-fn, pre.source-code .code-fn, pre code .code-fn { color: #14710A; }
                                                            pre .code-method, pre.source-code .code-method, pre code .code-method { color: #198D0C; }
                                                            pre .code-cls, pre.source-code .code-cls, pre code .code-cls { color: #036A96; font-weight: 600; }
                                                            pre .code-dec, pre.source-code .code-dec, pre code .code-dec { color: #a8700a; }
                                                            pre .code-builtin, pre.source-code .code-builtin, pre code .code-builtin { color: #644AC9; }
                                                            pre .code-op, pre.source-code .code-op, pre code .code-op { color: #BF185A; }
                                                            """;

    public static readonly double DefaultFontSize = 16.0;
    public static readonly double DefaultLineHeight = 1.6;

    // Predefined color schemes for reading
    public static readonly ColorScheme LightColorScheme = new(
        "#FFFFFF",
        "#1A1A1A",
        "#0066CC",
        "#B4D5FF",
        "#F5F5F5",
        "#E0E0E0");

    public static readonly ColorScheme DraculaColorScheme = new(
        "#0e0d11",
        "#F8F8F2",
        "#8BE9FD",
        "#44475A",
        "#282A36",
        "#383645");

    public static readonly ColorScheme SepiaColorScheme = new(
        "#F4ECD8",
        "#3B2A1A",
        "#8B4513",
        "#D4B896",
        "#EBE3D0",
        "#D4C4A8");

    // Private parameterless constructor for EF Core
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor
    private CssStyle()
    {
        // EF Core will set properties directly
    }
#pragma warning restore CS8618

    public string FontFamily { get; set; }
    public double FontSize { get; set; }
    public double LineHeight { get; set; }
    public double LetterSpacing { get; set; }
    public int MarginHorizontal { get; set; }
    public int MarginVertical { get; set; }
    public ColorScheme Colors { get; set; }
    public string? CustomCss { get; set; }

    public static CssStyle Default => new()
    {
        FontFamily = "Segoe UI, sans-serif",
        FontSize = DefaultFontSize,
        LineHeight = DefaultLineHeight,
        LetterSpacing = 0,
        MarginHorizontal = 40,
        MarginVertical = 20,
        Colors = LightColorScheme,
        CustomCss = SyntaxHighlightDraculaAlucardCss
    };

    public static CssStyle Dracula => new()
    {
        FontFamily = "Segoe UI, sans-serif",
        FontSize = DefaultFontSize,
        LineHeight = DefaultLineHeight,
        LetterSpacing = 0,
        MarginHorizontal = 40,
        MarginVertical = 20,
        Colors = DraculaColorScheme,
        CustomCss = SyntaxHighlightDraculaColorfulCss
    };

    public static CssStyle Sepia => new()
    {
        FontFamily = "Georgia, serif",
        FontSize = DefaultFontSize,
        LineHeight = DefaultLineHeight,
        LetterSpacing = 0.1,
        MarginHorizontal = 42,
        MarginVertical = 22,
        Colors = SepiaColorScheme,
        CustomCss = SyntaxHighlightDraculaAlucardCss
    };

    /// <summary>
    ///     Renders a complete CSS stylesheet for EPUB content rendering.
    ///     Includes colors, typography, and layout.
    /// </summary>
    private string ToCssVariables()
    {
        return $$"""
                 :root {
                     --bg-color: {{Colors.Background}} ;
                     --text-color: {{Colors.Text}};
                     --link-color: {{Colors.Link}};
                     --selection-color: {{Colors.Selection}};
                     --surface-color: {{Colors.Surface}};
                     --border-color: {{Colors.Border}};
                 }
                 """;
    }

    /// <summary>
    ///     Renders a complete CSS stylesheet for EPUB content rendering.
    ///     Includes colors, typography, and layout.
    /// </summary>
    public string ToStylesheet()
    {
        var baseStyles = $$"""

                           {{ToCssVariables()}}

                           /* Base typography and layout */
                           body {
                               background-color: var(--bg-color);
                               color: var(--text-color);
                               font-family: {{FontFamily}};
                               font-size: {{FontSize}}px;
                               line-height: {{LineHeight}};
                               letter-spacing: {{LetterSpacing}}px;
                               margin: {{MarginVertical}}px {{MarginHorizontal}}px;
                               padding: 20px;
                               max-width: 800px;
                               margin-left: auto;
                               margin-right: auto;
                           }

                           /* Links */
                           a {
                               color: var(--link-color);
                               text-decoration: none;
                           }

                           a:hover {
                               text-decoration: underline;
                           }

                           /* Text selection */
                           ::selection {
                               background-color: var(--selection-color);
                               color: var(--text-color);
                           }

                           /* Images */
                           img {
                               max-width: 100%;
                               height: auto;
                               display: block;
                               margin: 1em auto;
                           }

                           /* Code blocks */
                           pre, code {
                               background-color: var(--surface-color);
                               border: 1px solid var(--border-color);
                               border-radius: 4px;
                               font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
                               font-size: 0.9em;
                           }

                           code {
                               padding: 2px 6px;
                           }

                           pre {
                               padding: 12px;
                               overflow-x: auto;
                               line-height: 1.4;
                           }

                           pre code {
                               padding: 0;
                               border: none;
                               background: none;
                           }

                           /* Blockquotes */
                           blockquote {
                               border-left: 4px solid var(--border-color);
                               padding-left: 16px;
                               margin-left: 0;
                               font-style: italic;
                               opacity: 0.9;
                           }

                           /* Headings */
                           h1, h2, h3, h4, h5, h6 {
                               color: var(--text-color);
                               margin-top: 1.5em;
                               margin-bottom: 0.5em;
                               line-height: 1.3;
                           }

                           /* Paragraphs */
                           p {
                               margin: 1em 0;
                           }

                           /* Lists */
                           ul, ol {
                               margin: 1em 0;
                               padding-left: 2em;
                           }

                           /* Tables */
                           table {
                               border-collapse: collapse;
                               width: 100%;
                               margin: 1em 0;
                           }

                           th, td {
                               border: 1px solid var(--border-color);
                               padding: 8px 12px;
                               text-align: left;
                           }

                           th {
                               background-color: var(--surface-color);
                               font-weight: bold;
                           }

                           /* Horizontal rules */
                           hr {
                               border: none;
                               border-top: 1px solid var(--border-color);
                               margin: 2em 0;
                           }

                           """;

        return CustomCss != null
            ? baseStyles + "\n\n/* Custom CSS */\n" + CustomCss
            : baseStyles;
    }
}