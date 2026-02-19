namespace DesktopUI.ViewModels;

public sealed class BookDetailsViewModel
{
    public string Title { get; init; } = string.Empty;
    public string Authors { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;
    public string Publisher { get; init; } = string.Empty;
    public string Isbn { get; init; } = string.Empty;
    public string PublishedDate { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Rights { get; init; } = string.Empty;
    public string EpubVersion { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string DescriptionHtmlDocument { get; init; } = "<html><body></body></html>";
}