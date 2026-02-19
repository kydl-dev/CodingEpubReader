using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Configuration;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Logs;
using Shared.BackgroundWorkers.Utils;
using Shared.Exceptions;

namespace Shared.BackgroundWorkers;

/// <summary>
///     Background worker that periodically indexes logs and caches error types and statistics
///     for dashboard and search displaying
/// </summary>
public partial class LoggingStatisticsWorker(
    ILogStatisticsService statisticsService,
    ILogFileProvider logFileProvider,
    ILoggingStatisticsConfiguration configuration,
    ILogger<LoggingStatisticsWorker> logger)
    : BackgroundWorkerBase<LoggingStatisticsWorker>(logger)
{
    private readonly ILoggingStatisticsConfiguration _configuration =
        configuration ?? throw new ArgumentNullException(nameof(configuration));

    private readonly ILogFileProvider _logFileProvider =
        logFileProvider ?? throw new ArgumentNullException(nameof(logFileProvider));

    private readonly ILogStatisticsService _statisticsService =
        statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));

    public override string WorkerName => "Logging Statistics Worker";

    // Run every 15 minutes by default
    protected override TimeSpan ExecutionInterval => _configuration.AggregationInterval;

    protected override bool RunImmediatelyOnStartup => false;

    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsEnabled)
        {
            LoggingStatisticsWorkerLogs.Disabled(Logger);
            return;
        }

        LoggingStatisticsWorkerLogs.Started(Logger);

        try
        {
            var startTime = DateTime.UtcNow;
            var logFiles = _logFileProvider.GetLogFiles(_configuration.LogRetentionDays);
            var logFilesList = logFiles.ToList();

            if (!logFilesList.Any())
            {
                LoggingStatisticsWorkerLogs.NoLogFilesFound(Logger);
                return;
            }

            LoggingStatisticsWorkerLogs.ProcessingFiles(Logger, logFilesList.Count);

            var statistics = new LogStatistics
            {
                StartTime = startTime,
                ProcessedFiles = 0,
                TotalErrors = 0,
                TotalWarnings = 0,
                TotalInformation = 0,
                ErrorsByType = new Dictionary<string, int>(),
                ErrorsByMessage = new Dictionary<string, ErrorMessageInfo>(),
                TimeSeriesData = new Dictionary<DateTime, LogTimeSeries>()
            };

            foreach (var logFile in logFilesList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessLogFileAsync(logFile, statistics, cancellationToken);
                    statistics.ProcessedFiles++;
                }
                catch (Exception ex)
                {
                    LoggingStatisticsWorkerLogs.ErrorProcessingLogFile(Logger, ex, logFile, ex.FullMessage());
                }
            }

            // Calculate additional statistics
            statistics.EndTime = DateTime.UtcNow;
            statistics.ProcessingDuration = statistics.EndTime - statistics.StartTime;
            statistics.TopErrors = statistics.ErrorsByMessage
                .OrderByDescending(kvp => kvp.Value.Count)
                .Take(_configuration.TopErrorsCount)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Cache the statistics
            await _statisticsService.CacheStatisticsAsync(statistics, cancellationToken);

            LoggingStatisticsWorkerLogs.Completed(
                Logger,
                statistics.ProcessedFiles,
                statistics.TotalErrors,
                statistics.TotalWarnings,
                statistics.TotalInformation,
                statistics.ProcessingDuration);
        }
        catch (Exception ex)
        {
            LoggingStatisticsWorkerLogs.ErrorDuringAggregation(Logger, ex, ex.FullMessage());
            throw;
        }
    }

    private async Task ProcessLogFileAsync(
        string logFilePath,
        LogStatistics statistics,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(logFilePath))
        {
            LoggingStatisticsWorkerLogs.LogFileNotFound(Logger, logFilePath);
            return;
        }

        LoggingStatisticsWorkerLogs.ProcessingLogFile(Logger, logFilePath);

        using var reader = new StreamReader(logFilePath);
        var logEntryPattern = _configuration.GetLogEntryPattern();

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var logEntry = ParseLogEntry(line, logEntryPattern);
            if (logEntry == null)
                continue;

            // Update statistics based on log level
            switch (logEntry.Level)
            {
                case LogLevel.Error:
                    statistics.TotalErrors++;
                    UpdateErrorStatistics(statistics, logEntry);
                    break;
                case LogLevel.Warning:
                    statistics.TotalWarnings++;
                    break;
                case LogLevel.Information:
                    statistics.TotalInformation++;
                    break;
                case LogLevel.Trace:
                case LogLevel.Debug:
                case LogLevel.Critical:
                case LogLevel.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update time series data
            UpdateTimeSeriesData(statistics, logEntry);
        }
    }

    private LogEntry? ParseLogEntry(string line, Regex pattern)
    {
        var match = pattern.Match(line);
        if (!match.Success)
            return null;

        try
        {
            return new LogEntry
            {
                Timestamp = DateTime.Parse(match.Groups["timestamp"].Value),
                Level = ParseLogLevel(match.Groups["level"].Value),
                Message = match.Groups["message"].Value,
                Exception = match.Groups["exception"].Value
            };
        }
        catch
        {
            return null;
        }
    }

    private LogLevel ParseLogLevel(string level)
    {
        return level.ToUpperInvariant() switch
        {
            "ERROR" or "ERR" => LogLevel.Error,
            "WARNING" or "WARN" or "WRN" => LogLevel.Warning,
            "INFORMATION" or "INFO" or "INF" => LogLevel.Information,
            "DEBUG" or "DBG" => LogLevel.Debug,
            _ => LogLevel.None
        };
    }

    private void UpdateErrorStatistics(LogStatistics statistics, LogEntry logEntry)
    {
        // Extract error type from exception or message
        var errorType = ExtractErrorType(logEntry);

        statistics.ErrorsByType.TryAdd(errorType, 0);
        statistics.ErrorsByType[errorType]++;

        // Track error messages
        var errorKey = logEntry.Message.Length > 200
            ? logEntry.Message[..200]
            : logEntry.Message;

        if (!statistics.ErrorsByMessage.ContainsKey(errorKey))
            statistics.ErrorsByMessage[errorKey] = new ErrorMessageInfo
            {
                Message = errorKey,
                ErrorType = errorType,
                FirstOccurrence = logEntry.Timestamp,
                Count = 0
            };

        statistics.ErrorsByMessage[errorKey].Count++;
        statistics.ErrorsByMessage[errorKey].LastOccurrence = logEntry.Timestamp;
    }

    private static string ExtractErrorType(LogEntry logEntry)
    {
        if (!string.IsNullOrEmpty(logEntry.Exception))
        {
            // Try to extract exception type from the exception string
            var match = GeneratedRegex().Match(logEntry.Exception);
            if (match.Success)
                return match.Groups[1].Value;
        }

        // Fall back to categorizing by message keywords
        if (logEntry.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return "NotFoundException";
        if (logEntry.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
            return "UnauthorizedException";
        if (logEntry.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
            return "ValidationException";
        return logEntry.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            ? "TimeoutException"
            : "UnknownError";
    }

    private static void UpdateTimeSeriesData(LogStatistics statistics, LogEntry logEntry)
    {
        var hourKey = new DateTime(
            logEntry.Timestamp.Year,
            logEntry.Timestamp.Month,
            logEntry.Timestamp.Day,
            logEntry.Timestamp.Hour,
            0, 0);

        if (!statistics.TimeSeriesData.TryGetValue(hourKey, out var timeSeries))
        {
            timeSeries = new LogTimeSeries
            {
                Timestamp = hourKey
            };
            statistics.TimeSeriesData[hourKey] = timeSeries;
        }

        switch (logEntry.Level)
        {
            case LogLevel.Error:
                timeSeries.ErrorCount++;
                break;
            case LogLevel.Warning:
                timeSeries.WarningCount++;
                break;
            case LogLevel.Information:
                timeSeries.InfoCount++;
                break;
            case LogLevel.Trace:
            case LogLevel.Debug:
            case LogLevel.Critical:
            case LogLevel.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [GeneratedRegex(@"(\w+Exception)")]
    private static partial Regex GeneratedRegex();
}