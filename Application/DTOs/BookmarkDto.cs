using Domain.Enums;

namespace Application.DTOs;

public record BookmarkDto(
    Guid Id,
    Guid BookId,
    string ChapterId,
    double Progress,
    BookmarkType Type,
    string? Note,
    DateTime CreatedAt);