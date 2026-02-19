using Microsoft.Extensions.Logging;

namespace Shared.BackgroundWorkers.Logs;

internal static partial class ReadingSessionTrackingWorkerLogs
{
    [LoggerMessage(
        EventId = EventIdRange.ReadingSessionTrackingWorker + 0,
        Level = LogLevel.Debug,
        Message = "No active reading sessions to track")]
    public static partial void NoActiveSessions(
        ILogger<ReadingSessionTrackingWorker> logger);

    [LoggerMessage(
        EventId = EventIdRange.ReadingSessionTrackingWorker + 1,
        Level = LogLevel.Debug,
        Message = "Tracking {SessionCount} active reading sessions")]
    public static partial void TrackingSessions(
        ILogger<ReadingSessionTrackingWorker> logger,
        int sessionCount);

    [LoggerMessage(
        EventId = EventIdRange.ReadingSessionTrackingWorker + 2,
        Level = LogLevel.Debug,
        Message = "Updated reading session for book {BookId}, duration: {Duration}")]
    public static partial void SessionUpdated(
        ILogger<ReadingSessionTrackingWorker> logger,
        Guid bookId,
        TimeSpan duration);

    [LoggerMessage(
        EventId = EventIdRange.ReadingSessionTrackingWorker + 3,
        Level = LogLevel.Error,
        Message = "Error updating reading session for book {BookId}. Error: {Error}")]
    public static partial void ErrorUpdatingSession(
        ILogger<ReadingSessionTrackingWorker> logger,
        Exception exception,
        Guid bookId,
        string error);
}