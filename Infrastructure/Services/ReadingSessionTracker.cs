using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Utils;

namespace Infrastructure.Services;

/// <summary>
///     Tracks active reading sessions in memory
/// </summary>
public class ReadingSessionTracker(ILogger<ReadingSessionTracker> logger) : IReadingSessionTracker
{
    private readonly ConcurrentDictionary<Guid, ReadingSession> _activeSessions = new();
    private readonly ILogger<ReadingSessionTracker> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public IEnumerable<ReadingSession> GetActiveSessions()
    {
        // Clean up inactive sessions
        var inactiveSessions = _activeSessions
            .Where(kvp => !kvp.Value.IsActive())
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var bookId in inactiveSessions)
        {
            _activeSessions.TryRemove(bookId, out _);
            _logger.LogDebug("Removed inactive session for book {BookId}", bookId);
        }

        return _activeSessions.Values.Where(s => s.IsActive()).ToList();
    }

    public void StartSession(Guid bookId, string chapterId)
    {
        var session = new ReadingSession
        {
            BookId = bookId,
            CurrentChapterId = chapterId,
            CurrentPosition = 0,
            StartTime = DateTime.UtcNow,
            LastActivityTime = DateTime.UtcNow
        };

        _activeSessions.AddOrUpdate(bookId, session, (_, _) => session);
        _logger.LogInformation("Started reading session for book {BookId}", bookId);
    }

    public void UpdatePosition(Guid bookId, string chapterId, int position)
    {
        while (true)
        {
            if (_activeSessions.TryGetValue(bookId, out var session))
            {
                session.CurrentChapterId = chapterId;
                session.CurrentPosition = position;
                session.LastActivityTime = DateTime.UtcNow;
                _logger.LogDebug("Updated position for book {BookId}: Chapter={Chapter}, Position={Position}", bookId,
                    chapterId, position);
            }
            else
            {
                // Session doesn't exist, create it
                StartSession(bookId, chapterId);
                continue;
            }

            break;
        }
    }

    public void EndSession(Guid bookId)
    {
        if (_activeSessions.TryRemove(bookId, out var session))
            _logger.LogInformation("Ended reading session for book {BookId}, duration: {Duration}",
                bookId, session.ReadingDuration);
    }
}