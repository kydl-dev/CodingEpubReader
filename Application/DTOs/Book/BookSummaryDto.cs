namespace Application.DTOs.Book;

/// <summary>
///     Lightweight projection used for the library grid/list view.
///     Does not include chapters or full metadata to keep queries fast.
/// </summary>
public class BookSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string PrimaryAuthor { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public DateTime AddedDate { get; set; }
    public DateTime? LastOpenedDate { get; set; }
    public double OverallProgress { get; set; } = 0.0;
}