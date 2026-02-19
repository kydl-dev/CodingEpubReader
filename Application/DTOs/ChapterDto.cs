namespace Application.DTOs;

public record ChapterDto(
    string Id,
    string Title,
    string HtmlContent,
    int Order,
    IReadOnlyList<string> CssResources);