namespace Infrastructure.Entities;

public class BookStatistics
{
    public int TotalChapters { get; set; }
    public int TotalWords { get; set; }
    public int EstimatedReadingTimeMinutes { get; set; }
    public bool HasCover { get; set; }
    public string Language { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
}