namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Interface for library scanning service
/// </summary>
public interface ILibraryScanningService
{
    Task<int> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default);
}