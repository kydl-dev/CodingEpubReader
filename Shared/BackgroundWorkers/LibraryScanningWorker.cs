using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Configuration;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Logs;
using Shared.Exceptions;

namespace Shared.BackgroundWorkers;

/// <summary>
///     Background worker that periodically scans configured folders for new EPUB files
///     and automatically adds them to the library
/// </summary>
public class LibraryScanningWorker(
    ILibraryScanningService libraryService,
    ILibraryScanConfiguration configuration,
    ILogger<LibraryScanningWorker> logger)
    : BackgroundWorkerBase<LibraryScanningWorker>(logger)
{
    private readonly ILibraryScanConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly ILibraryScanningService _libraryService =
        libraryService ?? throw new ArgumentNullException(nameof(libraryService));

    public override string WorkerName => "Library Scanning Worker";

    protected override TimeSpan ExecutionInterval => _configuration.ScanInterval;

    protected override bool RunImmediatelyOnStartup => _configuration.ScanOnStartup;

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            LibraryScanningWorkerLogs.Disabled(Logger);
            return;
        }

        var watchedFolders = _configuration.GetWatchedFolders();
        var enumerable = watchedFolders.ToList();
        if (!enumerable.Any())
        {
            LibraryScanningWorkerLogs.NoWatchedFolders(Logger);
            return;
        }

        LibraryScanningWorkerLogs.Started(Logger, enumerable.Count);

        var totalImported = 0;
        var totalErrors = 0;

        foreach (var folder in enumerable)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var folderPath = folder ?? string.Empty;

            try
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                {
                    LibraryScanningWorkerLogs.FolderNotFound(Logger, folderPath);
                    continue;
                }

                LibraryScanningWorkerLogs.ScanningFolder(Logger, folderPath);
                var importedCount = await _libraryService.ImportFolderAsync(folderPath, cancellationToken);
                totalImported += importedCount;

                LibraryScanningWorkerLogs.FolderImportCompleted(Logger, importedCount, folderPath);
            }
            catch (Exception ex)
            {
                totalErrors++;
                LibraryScanningWorkerLogs.ErrorScanningFolder(Logger, ex, folderPath, ex.FullMessage());
            }
        }

        LibraryScanningWorkerLogs.Completed(Logger, totalImported, totalErrors);
    }
}