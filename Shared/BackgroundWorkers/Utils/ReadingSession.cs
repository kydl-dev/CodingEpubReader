namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Represents an active reading session
/// </summary>
public class ReadingSession
{
    private string? _lastTrackedChapterId;
    private int _lastTrackedPosition;

    public Guid BookId { get; init; }
    public string CurrentChapterId { get; set; } = string.Empty;
    public int CurrentPosition { get; set; }
    public DateTime StartTime { get; init; }
    public DateTime LastActivityTime { get; set; }

    /// <summary>
    ///     Gets the duration of the current reading session
    /// </summary>
    public TimeSpan ReadingDuration => DateTime.UtcNow - StartTime;

    /// <summary>
    ///     Checks if the reading progress has changed since the last tracking
    /// </summary>
    public bool HasProgressChanged()
    {
        var changed = _lastTrackedChapterId != CurrentChapterId ||
                      _lastTrackedPosition != CurrentPosition;

        if (!changed) return changed;
        _lastTrackedChapterId = CurrentChapterId;
        _lastTrackedPosition = CurrentPosition;

        return changed;
    }

    /// <summary>
    ///     Checks if the session is still active (activity within last 5 minutes)
    /// </summary>
    public bool IsActive()
    {
        return DateTime.UtcNow - LastActivityTime < TimeSpan.FromMinutes(5);
    }
}