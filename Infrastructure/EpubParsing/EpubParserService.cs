using Application.Exceptions;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using VersOne.Epub;
using VersOne.Epub.Schema;
using EpubMetadata = Domain.ValueObjects.EpubMetadata;
using Reader = VersOne.Epub.EpubReader;

namespace Infrastructure.EpubParsing;

public class EpubParserService(ILogger<EpubParserService> logger) : IEpubParser
{
    private readonly ILogger<EpubParserService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Book> ParseAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Epub file not found at path: {filePath}", filePath);

        try
        {
            _logger.LogInformation("Parsing epub file: {FilePath}", filePath);

            var epubBook = await Reader.ReadBookAsync(filePath);

            var metadata = ExtractMetadata(epubBook);
            var chapters = ExtractChapters(epubBook).ToList();
            var tableOfContents = ExtractTableOfContents(epubBook).ToList();

            var book = Book.Create(
                epubBook.Title,
                epubBook.AuthorList,
                epubBook.Schema.Package.Metadata.Languages.FirstOrDefault()?.Language ?? "en",
                metadata,
                chapters,
                tableOfContents,
                filePath);

            _logger.LogInformation("Successfully parsed epub: {Title} with {ChapterCount} chapters",
                book.Title, chapters.Count);

            return book;
        }
        catch (Exception ex) when (ex is not InvalidEpubFormatException)
        {
            _logger.LogError(ex, "Failed to parse epub file: {FilePath}. Error: {Error}", filePath, ex.FullMessage());
            throw new InvalidEpubFormatException($"Failed to parse epub file: {ex.Message}", ex.FullMessage());
        }
    }

    public bool IsSupported(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return false;

        if (!File.Exists(filePath))
            return false;

        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".epub";
    }

    private EpubMetadata ExtractMetadata(EpubBook epubBook)
    {
        var metadata = epubBook.Schema.Package.Metadata;

        string? coverImagePath = null;
        try
        {
            var coverImage = epubBook.Content.Cover;
            if (coverImage != null) coverImagePath = coverImage.FilePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract cover image. Error: {Error}", ex.FullMessage());
        }

        return new EpubMetadata(
            metadata.Publishers.FirstOrDefault()?.Publisher,
            metadata.Descriptions.FirstOrDefault()?.Description,
            metadata.Creators.Select(c => c.Creator).ToList(),
            metadata.Identifiers.FirstOrDefault(id => id.Scheme?.ToLower() == "isbn")?.Identifier,
            uuid: metadata.Identifiers.FirstOrDefault()?.Identifier,
            subject: metadata.Subjects.FirstOrDefault()?.Subject,
            rights: metadata.Rights.FirstOrDefault()?.Rights,
            publishedDate: TryParseDate(metadata.Dates.FirstOrDefault()?.Date),
            coverImagePath: coverImagePath,
            format: BookFormat.Epub,
            epubVersion: epubBook.Schema.Package.EpubVersion.ToString());
    }

    private IEnumerable<ChapterImpl> ExtractChapters(EpubBook epubBook)
    {
        var readingOrder = epubBook.Schema.Package.Spine.Items;
        var order = 0;

        foreach (var htmlFile in readingOrder.Select(spineItem => epubBook.Schema.Package.Manifest.Items
                     .FirstOrDefault(m => m.Id == spineItem.IdRef)).OfType<EpubManifestItem>().Select(manifestItem =>
                     epubBook.Content.Html.Local
                         .FirstOrDefault(h =>
                             h.FilePath == manifestItem.Href ||
                             h.FilePath.EndsWith("/" + manifestItem.Href, StringComparison.OrdinalIgnoreCase))))
        {
            if (htmlFile?.Content == null) continue;
            var cssResources = epubBook.Content.Css.Local
                .Select(css => css.Content)
                .ToList();

            // FIX: Use htmlFile.FilePath (the full resolved path, e.g. "OEBPS/chapter01.xhtml")
            // as the chapter ID. The TOC's ContentSrc is also the full resolved path, so
            // both will match when BookContentService calls GetChapterById(contentSrc).
            yield return new ChapterImpl(
                htmlFile.FilePath,
                FindChapterTitle(htmlFile.FilePath, epubBook) ?? $"Chapter {order + 1}",
                htmlFile.Content,
                order++,
                cssResources);
        }
    }

    private static string? FindChapterTitle(string filePath, EpubBook epubBook)
    {
        return epubBook.Navigation?
            .Select(navItem => FindTitleInNavigation(navItem, filePath))
            .OfType<string>()
            .FirstOrDefault();
    }

    private static string? FindTitleInNavigation(EpubNavigationItem navItem, string filePath)
    {
        // FIX: Use HtmlContentFile.FilePath as primary — it's fully resolved and reliable.
        var itemFilePath = navItem.HtmlContentFile?.FilePath ?? navItem.Link?.ContentFilePath;

        if (itemFilePath != null &&
            itemFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
            return navItem.Title;

        return navItem.NestedItems
            .Select(nested => FindTitleInNavigation(nested, filePath))
            .OfType<string>()
            .FirstOrDefault();
    }

    private static IEnumerable<TocItem> ExtractTableOfContents(EpubBook epubBook)
    {
        if (epubBook.Navigation == null)
            yield break;

        var playOrder = 0;
        foreach (var navItem in epubBook.Navigation) yield return ConvertNavItem(navItem, 0, ref playOrder);
    }

    private static TocItem ConvertNavItem(EpubNavigationItem navItem, int depth, ref int playOrder)
    {
        var currentOrder = playOrder++;

        var children = new List<TocItem>();
        foreach (var nested in navItem.NestedItems) children.Add(ConvertNavItem(nested, depth + 1, ref playOrder));

        var contentSrc = BuildContentSrc(navItem);

        return new TocItem(
            !string.IsNullOrEmpty(contentSrc) ? contentSrc : Guid.NewGuid().ToString(),
            navItem.Title,
            contentSrc,
            currentOrder,
            depth,
            children);
    }

    private static string BuildContentSrc(EpubNavigationItem navItem)
    {
        var linkPath = navItem.Link?.ContentFilePath ?? string.Empty;
        var linkAnchor = navItem.Link?.Anchor ?? string.Empty;
        var htmlFilePath = navItem.HtmlContentFile?.FilePath ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(linkPath))
        {
            var hasAnchorInPath = linkPath.Contains('#');
            var normalizedPath = NormalizeContentPath(linkPath, htmlFilePath);
            if (hasAnchorInPath || string.IsNullOrWhiteSpace(linkAnchor)) return normalizedPath;

            return $"{normalizedPath}#{linkAnchor.TrimStart('#')}";
        }

        if (string.IsNullOrWhiteSpace(htmlFilePath)) return string.Empty;

        if (string.IsNullOrWhiteSpace(linkAnchor)) return htmlFilePath;

        return $"{htmlFilePath}#{linkAnchor.TrimStart('#')}";
    }

    private static string NormalizeContentPath(string path, string referencePath)
    {
        var trimmed = path.Trim().Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(trimmed)) return string.Empty;

        var hashIndex = trimmed.IndexOf('#');
        var queryIndex = trimmed.IndexOf('?');
        var splitIndex = hashIndex >= 0 && queryIndex >= 0
            ? Math.Min(hashIndex, queryIndex)
            : Math.Max(hashIndex, queryIndex);

        var suffix = splitIndex >= 0 ? trimmed[splitIndex..] : string.Empty;
        var basePath = splitIndex >= 0 ? trimmed[..splitIndex] : trimmed;

        if (!string.IsNullOrWhiteSpace(referencePath) &&
            !basePath.StartsWith("/", StringComparison.Ordinal) &&
            !basePath.Contains("/", StringComparison.Ordinal))
        {
            var referenceDirectory = Path.GetDirectoryName(referencePath.Replace('\\', '/'))
                ?.Replace('\\', '/');
            if (!string.IsNullOrWhiteSpace(referenceDirectory)) basePath = $"{referenceDirectory}/{basePath}";
        }

        var normalized = NormalizePathSegments(basePath);
        return normalized + suffix;
    }

    private static string NormalizePathSegments(string path)
    {
        var normalized = path.Replace('\\', '/');
        while (normalized.Contains("//", StringComparison.Ordinal))
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);

        if (normalized.StartsWith("/", StringComparison.Ordinal)) normalized = normalized[1..];

        var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var stack = new Stack<string>();
        foreach (var part in parts)
        {
            if (part == ".") continue;

            if (part == "..")
            {
                if (stack.Count > 0) stack.Pop();
                continue;
            }

            stack.Push(part);
        }

        return string.Join("/", stack.Reverse());
    }

    private static DateTime? TryParseDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        if (DateTime.TryParse(dateString, out var date))
            return date;

        return null;
    }

    private class ChapterImpl(
        string id,
        string title,
        string htmlContent,
        int order,
        IEnumerable<string>? cssResources = null)
        : Chapter(id, title, htmlContent, order, cssResources);
}