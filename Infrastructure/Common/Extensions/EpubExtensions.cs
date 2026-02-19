using System.Text.RegularExpressions;
using Domain.Entities;

namespace Infrastructure.Common.Extensions;

public static class EpubExtensions
{
    /// <summary>
    ///     Strips HTML tags from a string
    /// </summary>
    private static string StripHtml(this string htmlContent)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return string.Empty;

        return Regex.Replace(htmlContent, "<[^>]+>", " ")
            .Replace("&nbsp;", " ")
            .Replace("&quot;", "\"")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">")
            .Trim();
    }

    extension(Chapter chapter)
    {
        /// <summary>
        ///     Extracts plain text content from HTML
        /// </summary>
        public string GetPlainText()
        {
            if (chapter == null)
                throw new ArgumentNullException(nameof(chapter));

            return chapter.HtmlContent.StripHtml();
        }

        /// <summary>
        ///     Gets a preview of chapter content (first N characters)
        /// </summary>
        public string GetPreview(int maxLength = 200)
        {
            if (chapter == null)
                throw new ArgumentNullException(nameof(chapter));

            var plainText = chapter.GetPlainText();

            if (plainText.Length <= maxLength)
                return plainText;

            return plainText.Substring(0, maxLength) + "...";
        }
    }

    extension(Book book)
    {
        /// <summary>
        ///     Calculates the total reading time for a book
        /// </summary>
        public int EstimateTotalReadingTime(int wordsPerMinute = 200)
        {
            if (book == null)
                throw new ArgumentNullException(nameof(book));

            return book.Chapters.Sum(chapter =>
            {
                var wordCount = chapter.EstimatedWordCount;
                return Math.Max(1, (int)Math.Ceiling((double)wordCount / wordsPerMinute));
            });
        }

        /// <summary>
        ///     Gets the total word count across all chapters
        /// </summary>
        public int GetTotalWordCount()
        {
            return book == null
                ? throw new ArgumentNullException(nameof(book))
                : book.Chapters.Sum(chapter => chapter.EstimatedWordCount);
        }
    }
}