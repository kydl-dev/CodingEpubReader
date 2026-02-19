using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Interfaces;

/// <summary>
///     Service for managing Avalonia UI theme settings.
///     This controls the application interface appearance only.
///     For book content styling, see CssStyle and SavedCssStyle.
/// </summary>
public interface IThemeService
{
    /// <summary>
    ///     Gets the current UI theme.
    /// </summary>
    Theme CurrentTheme { get; }

    /// <summary>
    ///     Sets the UI theme and persists the preference.
    /// </summary>
    Task SetThemeAsync(ThemeKind kind, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Loads the saved theme preference from settings.
    ///     If no preference is saved, returns the default theme (Dark).
    /// </summary>
    Task<Theme> LoadSavedThemeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Event raised when the UI theme changes.
    ///     UI components can subscribe to update their appearance.
    /// </summary>
    event EventHandler<Theme>? ThemeChanged;
}