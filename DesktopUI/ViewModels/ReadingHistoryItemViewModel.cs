using System;

namespace DesktopUI.ViewModels;

public sealed class ReadingHistoryItemViewModel
{
    public Guid? BookId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public DateTime LastReadAt { get; init; }
    public TimeSpan TotalReadingTime { get; init; }
    public int TotalSessions { get; init; }
    public bool CanOpen => BookId.HasValue;
}