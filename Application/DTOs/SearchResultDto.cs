namespace Application.DTOs;

public record SearchResultDto(
    string ChapterId,
    string ChapterTitle,
    int ChapterOrder,
    int Position,
    string MatchedText,
    string BeforeContext,
    string AfterContext)
{
    public string GetPreview(int maxLength = 200)
    {
        var full = $"{BeforeContext}{MatchedText}{AfterContext}";
        if (full.Length <= maxLength) return full;

        var halfLength = maxLength / 2;
        var start = Math.Max(0, BeforeContext.Length - halfLength);
        var end = Math.Min(full.Length, BeforeContext.Length + MatchedText.Length + halfLength);
        var preview = full[start..end];

        if (start > 0) preview = "..." + preview;
        if (end < full.Length) preview += "...";

        return preview;
    }
}