using Shared.BackgroundWorkers.Utils;

namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Interface for accessing book repository (simplified for this worker)
/// </summary>
public interface IBookRepositoryWorker
{
    Task<IEnumerable<IBookCoverInfo>> GetBooksNeedingThumbnailsAsync(CancellationToken cancellationToken = default);
}