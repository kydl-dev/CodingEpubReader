using System.Text.RegularExpressions;

namespace Domain.Entities;

public abstract class Chapter
{
    private Chapter()
    {
    }

    public Chapter(string id, string? title, string? htmlContent, int order, IEnumerable<string>? cssResources = null)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Chapter Id cannot be empty.", nameof(id));

        Id = id;
        Title = title ?? string.Empty;
        HtmlContent = htmlContent ?? string.Empty;
        Order = order;
        CssResources = cssResources?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    public string Id { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public int Order { get; private set; }
    public IReadOnlyList<string> CssResources { get; private set; } = [];

    public bool IsEmpty => string.IsNullOrWhiteSpace(HtmlContent);

    /// <summary>Estimates the word count by splitting the stripped HTML content.</summary>
    public int EstimatedWordCount =>
        Regex
            .Replace(HtmlContent, "<[^>]+>", " ")
            .Split((char[])null!, StringSplitOptions.RemoveEmptyEntries)
            .Length;
}