using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
///     Represents a saved CSS style configuration that can be applied to the reader.
///     Uses the CssStyle value object to store the actual style values.
/// </summary>
public class SavedCssStyle
{
    // EF Core / serialization constructor
    private SavedCssStyle()
    {
    }

    private SavedCssStyle(
        Guid id,
        string name,
        CssStyle style,
        bool isDefault)
    {
        Id = id;
        Name = name;
        Style = style;
        IsDefault = isDefault;
        CreatedAt = DateTime.UtcNow;
        LastModifiedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    /// <summary>
    ///     The user-friendly name for this style (e.g., "Night Reading", "Large Print", "Classic")
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    ///     The CSS style configuration
    /// </summary>
    public CssStyle Style { get; private set; } = CssStyle.Default;

    /// <summary>
    ///     Whether this is the default style to be applied when opening books
    /// </summary>
    public bool IsDefault { get; private set; }

    /// <summary>
    ///     When this style was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    ///     When this style was last modified
    /// </summary>
    public DateTime LastModifiedAt { get; private set; }

    public static SavedCssStyle Create(string name, CssStyle style, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Style name cannot be empty.", nameof(name));

        if (style == null)
            throw new ArgumentNullException(nameof(style));

        return new SavedCssStyle(
            Guid.NewGuid(),
            name,
            style,
            isDefault);
    }

    /// <summary>
    ///     Updates the style configuration
    /// </summary>
    public void UpdateStyle(CssStyle newStyle)
    {
        if (newStyle == null)
            throw new ArgumentNullException(nameof(newStyle));

        Style = newStyle;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Updates the name of this style
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Style name cannot be empty.", nameof(newName));

        Name = newName;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Sets this style as the default
    /// </summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        LastModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Removes the default flag from this style
    /// </summary>
    public void UnsetDefault()
    {
        IsDefault = false;
        LastModifiedAt = DateTime.UtcNow;
    }
}