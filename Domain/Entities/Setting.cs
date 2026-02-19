namespace Domain.Entities;

/// <summary>
///     Represents a key-value setting stored in the database
/// </summary>
public class Setting
{
    /// <summary>
    ///     Unique identifier for the setting (e.g., "WatchedFolders", "Theme", etc.)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     JSON-serialized value of the setting
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    ///     When the setting was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    ///     Optional description of what this setting does
    /// </summary>
    public string? Description { get; set; }
}