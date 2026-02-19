namespace Application.DTOs.Book;

/// <summary>Full book detail including chapters and TOC — used after opening a book.</summary>
public record BookDto(
    Guid Id,
    string Title,
    IReadOnlyList<string> Authors,
    string Language,
    MetadataDto Metadata,
    IReadOnlyList<ChapterDto> Chapters,
    IReadOnlyList<TocItemDto> TableOfContents,
    string FilePath,
    DateTime AddedDate,
    DateTime? LastOpenedDate);