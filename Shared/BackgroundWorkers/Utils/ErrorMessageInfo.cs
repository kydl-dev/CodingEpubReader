namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Information about a specific error message
/// </summary>
public class ErrorMessageInfo
{
    public string Message { get; set; } = string.Empty;
    public string ErrorType { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime FirstOccurrence { get; set; }
    public DateTime LastOccurrence { get; set; }
}