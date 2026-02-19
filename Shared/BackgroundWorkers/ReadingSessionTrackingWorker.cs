using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Logs;
using Shared.Exceptions;

namespace Shared.BackgroundWorkers;

/// <summary>
///     Background worker that automatically tracks reading sessions and updates reading history
/// </summary>
public class ReadingSessionTrackingWorker(
    IReadingSessionTracker sessionTracker,
    IReadingHistoryWorker historyWorker,
    ILogger<ReadingSessionTrackingWorker> logger)
    : BackgroundWorkerBase<ReadingSessionTrackingWorker>(logger)
{
    private readonly IReadingHistoryWorker _historyWorker =
        historyWorker ?? throw new ArgumentNullException(nameof(historyWorker));

    private readonly IReadingSessionTracker _sessionTracker =
        sessionTracker ?? throw new ArgumentNullException(nameof(sessionTracker));

    public override string WorkerName => "Reading Session Tracking Worker";

    // Track sessions every 30 seconds
    protected override TimeSpan ExecutionInterval => TimeSpan.FromSeconds(30);

    protected override bool RunImmediatelyOnStartup => false;

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var activeSessions = _sessionTracker.GetActiveSessions();
        var readingSessions = activeSessions.ToList();
        if (readingSessions.Count == 0)
        {
            ReadingSessionTrackingWorkerLogs.NoActiveSessions(Logger);
            return;
        }

        ReadingSessionTrackingWorkerLogs.TrackingSessions(Logger, readingSessions.Count);

        foreach (var session in readingSessions)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Update reading time
                await _historyWorker.UpdateReadingTimeAsync(
                    session.BookId,
                    session.ReadingDuration,
                    cancellationToken);

                // Update progress if changed
                if (session.HasProgressChanged())
                    await _historyWorker.UpdateProgressAsync(
                        session.BookId,
                        session.CurrentChapterId,
                        session.CurrentPosition,
                        cancellationToken);

                ReadingSessionTrackingWorkerLogs.SessionUpdated(Logger, session.BookId, session.ReadingDuration);
            }
            catch (Exception ex)
            {
                ReadingSessionTrackingWorkerLogs.ErrorUpdatingSession(Logger, ex, session.BookId, ex.FullMessage());
            }
        }
    }
}