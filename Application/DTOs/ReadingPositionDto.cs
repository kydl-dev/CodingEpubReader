namespace Application.DTOs;

public record ReadingPositionDto(
    Guid BookId,
    string ChapterId,
    double Progress,
    DateTime SavedAt);