using System.Net;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Book
{
    // Backing fields for EF Core JSON serialization
    private readonly List<ChapterData> _chapters = [];
    private readonly List<TocItem> _tableOfContents = [];

    // EF Core / serialization constructor
    private Book()
    {
    }

    private Book(
        BookId id,
        string title,
        IEnumerable<string> authors,
        string language,
        EpubMetadata metadata,
        IEnumerable<Chapter> chapters,
        IEnumerable<TocItem> tableOfContents,
        string filePath,
        DateTime addedDate)
    {
        Id = id;
        Title = title;
        Authors = authors.ToList().AsReadOnly();
        Language = language;
        Metadata = metadata;

        var chapterList = chapters.ToList();
        _chapters = chapterList.Select(c => new ChapterData
        {
            Id = c.Id,
            Title = c.Title,
            HtmlContent = c.HtmlContent,
            Order = c.Order,
            CssResources = c.CssResources.ToList()
        }).ToList();

        _tableOfContents = tableOfContents.ToList();
        FilePath = filePath;
        AddedDate = addedDate;
    }

    public BookId Id { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public IReadOnlyList<string> Authors { get; } = [];
    public string Language { get; private set; } = string.Empty;
    public EpubMetadata Metadata { get; } = EpubMetadata.Empty;

    public IReadOnlyList<Chapter> Chapters =>
        _chapters.Select(c => new ChapterImpl(c.Id, c.Title, c.HtmlContent, c.Order, c.CssResources)).ToList()
            .AsReadOnly();

    public IReadOnlyList<TocItem> TableOfContents => _tableOfContents.AsReadOnly();

    public string FilePath { get; private set; } = string.Empty;
    public DateTime AddedDate { get; private set; }
    public DateTime? LastOpenedDate { get; private set; }

    public bool HasCover => !string.IsNullOrEmpty(Metadata.CoverImagePath);

    public string PrimaryAuthor => Authors.FirstOrDefault() ?? "Unknown";

    public static Book Create(
        string title,
        IEnumerable<string>? authors,
        string? language,
        EpubMetadata? metadata,
        IEnumerable<Chapter>? chapters,
        IEnumerable<TocItem>? tableOfContents,
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Book title cannot be empty.", nameof(title));
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        return new Book(
            BookId.New(),
            title,
            authors ?? [],
            language ?? "en",
            metadata ?? EpubMetadata.Empty,
            chapters ?? [],
            tableOfContents ?? [],
            filePath,
            DateTime.UtcNow);
    }

    public void MarkAsOpened()
    {
        LastOpenedDate = DateTime.UtcNow;
    }

    public Chapter? GetChapterById(string chapterId)
    {
        if (string.IsNullOrWhiteSpace(chapterId)) return null;

        var normalizedChapterId = NormalizeChapterId(chapterId);
        return Chapters.FirstOrDefault(c =>
            ChapterIdMatches(NormalizeChapterId(c.Id), normalizedChapterId));
    }

    public int GetChapterIndex(string chapterId)
    {
        if (string.IsNullOrWhiteSpace(chapterId)) return -1;

        var normalizedChapterId = NormalizeChapterId(chapterId);
        return Chapters.ToList().FindIndex(c =>
            ChapterIdMatches(NormalizeChapterId(c.Id), normalizedChapterId));
    }

    public void UpdateTableOfContents(IEnumerable<TocItem> tableOfContents)
    {
        _tableOfContents.Clear();
        _tableOfContents.AddRange(tableOfContents);
    }

    /// <summary>
    ///     Replaces the stored chapters with a freshly parsed set.
    ///     Called by the healing path in GetTableOfContentsQueryHandler when the book
    ///     was originally imported with 0 chapters due to the path-matching bug.
    /// </summary>
    public void UpdateChapters(IEnumerable<Chapter> chapters)
    {
        var newChapterData = chapters.Select(c => new ChapterData
        {
            Id = c.Id,
            Title = c.Title,
            HtmlContent = c.HtmlContent,
            Order = c.Order,
            CssResources = c.CssResources.ToList()
        });

        _chapters.Clear();
        _chapters.AddRange(newChapterData);
    }

    private static string NormalizeChapterId(string chapterId)
    {
        var value = chapterId.Trim();

        var fragmentIndex = value.IndexOf('#');
        if (fragmentIndex >= 0) value = value[..fragmentIndex];

        value = value.Replace('\\', '/');

        if (value.StartsWith("./", StringComparison.Ordinal)) value = value[2..];

        if (value.StartsWith("/", StringComparison.Ordinal)) value = value[1..];

        return WebUtility.UrlDecode(value);
    }

    private static bool ChapterIdMatches(string storedChapterId, string requestedChapterId)
    {
        if (string.Equals(storedChapterId, requestedChapterId, StringComparison.OrdinalIgnoreCase)) return true;

        if (storedChapterId.EndsWith("/" + requestedChapterId, StringComparison.OrdinalIgnoreCase)) return true;

        if (requestedChapterId.EndsWith("/" + storedChapterId, StringComparison.OrdinalIgnoreCase)) return true;

        return false;
    }

    // Internal implementation of Chapter for reconstitution from database
    private class ChapterImpl(
        string id,
        string title,
        string htmlContent,
        int order,
        IEnumerable<string>? cssResources = null)
        : Chapter(id, title, htmlContent, order, cssResources);

    // DTO for JSON serialization (matches BookConfiguration.ChapterData)
    public class ChapterData
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public int Order { get; set; }
        public List<string> CssResources { get; init; } = [];
    }
}