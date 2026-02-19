namespace Shared.BackgroundWorkers.Utils;

/// <summary>
///     Simplified book interface for cover image operations
/// </summary>
public interface IBookCoverInfo
{
    Guid Id { get; }
    string Title { get; }
    string GetCoverImagePath();
}