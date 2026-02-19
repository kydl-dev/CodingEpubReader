using System;
using Avalonia.Media.Imaging;

namespace DesktopUI.ViewModels;

public class BookViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    /// <summary>First listed author � sourced from BookSummaryDto.PrimaryAuthor.</summary>
    public string PrimaryAuthor { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;
    public string? CoverImagePath { get; set; }
    public Bitmap? CoverPreviewImage { get; set; }
    public string Language { get; set; } = string.Empty;
    public DateTime AddedDate { get; set; }
    public DateTime? LastOpenedDate { get; set; }

    /// <summary>0�100 reading progress enriched by GetAllBooksQuery.</summary>
    public double OverallProgress { get; set; }
}