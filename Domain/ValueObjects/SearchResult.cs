namespace Domain.ValueObjects;

/// <summary>
///     Represents a single search result with context.
/// </summary>
public record SearchResult(
    string ChapterId,
    string ChapterTitle,
    int ChapterOrder,
    int Position,
    string MatchedText,
    string BeforeContext,
    string AfterContext);