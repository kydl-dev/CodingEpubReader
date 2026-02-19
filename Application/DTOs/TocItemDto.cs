namespace Application.DTOs;

public record TocItemDto(
    string Id,
    string Title,
    string ContentSrc,
    int PlayOrder,
    int Depth,
    IReadOnlyList<TocItemDto> Children);