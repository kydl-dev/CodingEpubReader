namespace DesktopUI.ViewModels;

public sealed class SearchFocusRequestViewModel
{
    public int RequestId { get; init; }
    public string MatchText { get; init; } = string.Empty;
    public int Position { get; init; }
}