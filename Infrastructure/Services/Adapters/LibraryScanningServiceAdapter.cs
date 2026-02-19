using Application.Interfaces;
using Shared.BackgroundWorkers.Interfaces;

namespace Infrastructure.Services.Adapters;

/// <summary>
///     Adapter to make LibraryService implement ILibraryScanningService
/// </summary>
public class LibraryScanningServiceAdapter(ILibraryService libraryService) : ILibraryScanningService
{
    private readonly ILibraryService _libraryService =
        libraryService ?? throw new ArgumentNullException(nameof(libraryService));

    public Task<int> ImportFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        return _libraryService.ImportFolderAsync(folderPath, cancellationToken);
    }
}