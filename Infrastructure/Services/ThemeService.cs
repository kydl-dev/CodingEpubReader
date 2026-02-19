using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class ThemeService(ISettingsRepository settingsRepository, ILogger<ThemeService> logger)
    : IThemeService
{
    private const string ThemeSettingKey = "UITheme";
    private readonly ILogger<ThemeService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ISettingsRepository _settingsRepository =
        settingsRepository ?? throw new ArgumentNullException(nameof(settingsRepository));

    // Default to Dark theme

    public Theme CurrentTheme { get; private set; } = Theme.Dark;

    public event EventHandler<Theme>? ThemeChanged;

    public async Task SetThemeAsync(ThemeKind kind, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Setting UI theme to: {ThemeKind}", kind);

        var newTheme = Theme.FromKind(kind);
        CurrentTheme = newTheme;

        // Raise event for UI to update
        ThemeChanged?.Invoke(this, newTheme);

        // Save to database
        try
        {
            var setting = new Setting
            {
                Key = ThemeSettingKey,
                Value = kind.ToString(),
                Description = "Avalonia UI theme preference (Light, Dark, or Sepia)",
                LastUpdated = DateTime.UtcNow
            };
            await _settingsRepository.UpsertAsync(setting, cancellationToken);

            _logger.LogInformation("UI theme preference saved: {ThemeName}", newTheme.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save UI theme preference");
        }
    }

    public async Task<Theme> LoadSavedThemeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading saved UI theme preference");

            var setting = await _settingsRepository.GetByKeyAsync(ThemeSettingKey, cancellationToken);

            if (setting == null)
            {
                _logger.LogInformation("No saved UI theme preference found, using default Dark theme");
                CurrentTheme = Theme.Dark;
                return CurrentTheme;
            }

            // Parse as ThemeKind enum
            if (Enum.TryParse<ThemeKind>(setting.Value, out var kind))
            {
                CurrentTheme = Theme.FromKind(kind);
                _logger.LogInformation("Loaded UI theme: {ThemeName}", CurrentTheme.Name);
                return CurrentTheme;
            }

            // Fallback to default
            _logger.LogWarning("Invalid theme value '{Value}', using default Dark theme", setting.Value);
            CurrentTheme = Theme.Dark;
            return CurrentTheme;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load saved UI theme, using default Dark theme");
            CurrentTheme = Theme.Dark;
            return CurrentTheme;
        }
    }
}