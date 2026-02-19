namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Interface for reading history service
/// </summary>
public interface IReadingHistoryWorker
{
    Task UpdateReadingTimeAsync(Guid bookId, TimeSpan duration, CancellationToken cancellationToken = default);

    Task UpdateProgressAsync(Guid bookId, string chapterId, int position,
        CancellationToken cancellationToken = default);
}