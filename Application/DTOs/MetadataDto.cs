using Domain.Enums;

namespace Application.DTOs;

public record MetadataDto(
    string? Publisher,
    string? Description,
    IReadOnlyList<string> Creators,
    string? Isbn,
    string? GoogleBooksId,
    string? CalibreId,
    string? Uuid,
    string? Subject,
    string? Rights,
    DateTime? PublishedDate,
    string? CoverImagePath,
    BookFormat Format,
    string? EpubVersion);