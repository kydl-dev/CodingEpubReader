namespace Application.DTOs;

public record HighlightDto(
    Guid Id,
    Guid BookId,
    string ChapterId,
    int StartOffset,
    int EndOffset,
    string SelectedText,
    string Color,
    string? Note,
    DateTime CreatedAt,
    DateTime? UpdatedAt);