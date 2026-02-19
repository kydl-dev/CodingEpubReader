using Microsoft.Extensions.Logging;

namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Represents a parsed log entry
/// </summary>
internal class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Exception { get; set; }
}