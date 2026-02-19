using Shared.BackgroundWorkers.Utils;

namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Interface for tracking reading sessions
/// </summary>
public interface IReadingSessionTracker
{
    /// <summary>
    ///     Gets all currently active reading sessions
    /// </summary>
    IEnumerable<ReadingSession> GetActiveSessions();

    /// <summary>
    ///     Starts a new reading session for a book
    /// </summary>
    void StartSession(Guid bookId, string chapterId);

    /// <summary>
    ///     Updates the current position in the reading session
    /// </summary>
    void UpdatePosition(Guid bookId, string chapterId, int position);

    /// <summary>
    ///     Ends a reading session
    /// </summary>
    void EndSession(Guid bookId);
}