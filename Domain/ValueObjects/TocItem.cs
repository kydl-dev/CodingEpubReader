using System.Collections.ObjectModel;

namespace Domain.ValueObjects;

/// <summary>
///     Table of contents Item
/// </summary>
public sealed record TocItem
{
    public TocItem(
        string id,
        string title,
        string contentSrc,
        int playOrder,
        int depth = 0,
        IEnumerable<TocItem>? children = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Table of contents Item Id cannot be null or whitespace.", nameof(id));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Table of contents Item Title cannot be null or whitespace.", nameof(title));

        // Note: contentSrc can be empty for grouping/header items that don't link to content
        ArgumentNullException.ThrowIfNull(contentSrc);

        Id = id;
        Title = title;
        ContentSrc = contentSrc;
        PlayOrder = playOrder;
        Depth = depth;
        Children = children?.ToList().AsReadOnly() ?? new List<TocItem>().AsReadOnly();
    }

    public string Id { get; }
    public string Title { get; }
    public string ContentSrc { get; }
    public int PlayOrder { get; }
    public int Depth { get; }
    public ReadOnlyCollection<TocItem> Children { get; }

    public bool HasChildren => Children.Count > 0;

    /// <summary>
    ///     Indicates whether this TOC item links to actual content (true) or is just a grouping/header item (false)
    /// </summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(ContentSrc);

    public IEnumerable<TocItem> Flatten()
    {
        yield return this;
        foreach (var child in Children)
        foreach (var item in child.Flatten())
            yield return item;
    }
}