using System.Net;
using System.Text.RegularExpressions;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Services;

/// <summary>
///     Domain service for searching and indexing text within book chapters.
///     Contains the core search logic and algorithms.
/// </summary>
public class SearchService
{
    /// <summary>
    ///     Searches for a query string across all chapters in a book.
    /// </summary>
    public IEnumerable<SearchResult> SearchInBook(
        Book book,
        string query,
        bool caseSensitive = false,
        bool wholeWord = false)
    {
        if (string.IsNullOrWhiteSpace(query))
            yield break;

        foreach (var chapter in book.Chapters)
        {
            var results = SearchInChapter(chapter, query, caseSensitive, wholeWord);
            foreach (var result in results) yield return result;
        }
    }

    private IEnumerable<SearchResult> SearchInChapter(
        Chapter chapter,
        string query,
        bool caseSensitive = false,
        bool wholeWord = false)
    {
        if (string.IsNullOrWhiteSpace(query) || chapter.IsEmpty)
            yield break;

        // Strip HTML tags for searching but keep track of positions
        var plainText = StripHtmlTags(chapter.HtmlContent);

        var comparison = caseSensitive
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;

        if (wholeWord)
        {
            // Use regex for whole word matching
            var pattern = $@"\b{Regex.Escape(query)}\b";
            var regexOptions = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            var matches = Regex.Matches(plainText, pattern, regexOptions);

            foreach (Match match in matches)
                yield return CreateSearchResult(chapter, match.Index, query.Length, plainText);
        }
        else
        {
            // Simple substring search
            var index = 0;
            while ((index = plainText.IndexOf(query, index, comparison)) != -1)
            {
                yield return CreateSearchResult(chapter, index, query.Length, plainText);
                index += query.Length;
            }
        }
    }

    private static SearchResult CreateSearchResult(
        Chapter chapter,
        int position,
        int matchLength,
        string plainText)
    {
        const int contextLength = 100;

        // Extract context around the match
        var startContext = Math.Max(0, position - contextLength);
        var endContext = Math.Min(plainText.Length, position + matchLength + contextLength);

        var beforeMatch = plainText.Substring(startContext, position - startContext);
        var match = plainText.Substring(position, matchLength);
        var afterMatch = plainText.Substring(position + matchLength, endContext - position - matchLength);

        // Trim to word boundaries for cleaner display
        beforeMatch = TrimToWordBoundary(beforeMatch, false);
        afterMatch = TrimToWordBoundary(afterMatch, true);

        return new SearchResult(
            chapter.Id,
            chapter.Title,
            chapter.Order,
            position,
            match,
            beforeMatch,
            afterMatch);
    }

    private static string StripHtmlTags(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        // Remove HTML tags but preserve text content
        var text = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"<style[^>]*>[\s\S]*?</style>", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, "<[^>]+>", " ");

        // Decode common HTML entities
        text = WebUtility.HtmlDecode(text);

        // Normalize whitespace
        text = Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }

    private static string TrimToWordBoundary(string text, bool fromStart)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        if (fromStart)
        {
            // Trim from start to first word boundary
            var match = Regex.Match(text, @"^\s*\S+\s+");
            return match.Success
                ? text[match.Length..]
                : text;
        }
        else
        {
            // Trim from end to last word boundary
            var match = Regex.Match(text, @"\s+\S+\s*$");
            return match.Success
                ? text[..match.Index]
                : text;
        }
    }
}