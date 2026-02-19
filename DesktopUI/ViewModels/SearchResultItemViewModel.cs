namespace DesktopUI.ViewModels;

public sealed class SearchResultItemViewModel
{
    public string ChapterId { get; set; } = string.Empty;
    public string ChapterTitle { get; set; } = string.Empty;
    public int ChapterOrder { get; set; }
    public int Position { get; set; }
    public string MatchedText { get; set; } = string.Empty;
    public string BeforeContext { get; set; } = string.Empty;
    public string AfterContext { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
}