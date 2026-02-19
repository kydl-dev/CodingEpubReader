using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Utils;

namespace Infrastructure.Services;

/// <summary>
///     Provides access to log files for processing
/// </summary>
public class LogFileProvider : ILogFileProvider
{
    private readonly string _logDirectory;
    private readonly ILogger<LogFileProvider> _logger;

    public LogFileProvider(ILogger<LogFileProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Must match Program.cs Serilog sink path.
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _logDirectory = Path.Combine(appDataPath, "EpubReader", "Logs");

        _logger.LogDebug("Log directory set to: {LogDirectory}", _logDirectory);
    }

    public IEnumerable<string> GetLogFiles(int maxAgeDays)
    {
        try
        {
            if (!Directory.Exists(_logDirectory))
            {
                _logger.LogWarning("Log directory does not exist: {LogDirectory}", _logDirectory);
                return [];
            }

            var threshold = DateTime.UtcNow.AddDays(-maxAgeDays);

            // Get all log files (assuming .log or .txt extension)
            var logFiles = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(_logDirectory, "*.txt", SearchOption.AllDirectories))
                .Select(f => new FileInfo(f))
                .Where(fi => fi.LastWriteTimeUtc >= threshold)
                .Select(fi => fi.FullName)
                .OrderByDescending(f => f) // Most recent first
                .ToList();

            _logger.LogDebug("Found {Count} log files within {Days} days", logFiles.Count, maxAgeDays);

            return logFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving log files from {LogDirectory}", _logDirectory);
            return [];
        }
    }
}