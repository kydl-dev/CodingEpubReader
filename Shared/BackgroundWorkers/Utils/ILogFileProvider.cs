namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Interface for providing log files
/// </summary>
public interface ILogFileProvider
{
    IEnumerable<string> GetLogFiles(int maxAgeDays);
}