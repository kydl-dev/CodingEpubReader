using Microsoft.Extensions.Hosting;

namespace Shared.BackgroundWorkers.Interfaces;

/// <summary>
///     Base interface for all background workers that perform periodic or long-running tasks
///     Extends IHostedService to allow registration as hosted services
/// </summary>
public interface IBackgroundWorker : IHostedService
{
    /// <summary>
    ///     Gets the name of the worker for identification and logging purposes
    /// </summary>
    string WorkerName { get; }

    /// <summary>
    ///     Executes the worker's task
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ExecuteAsync(CancellationToken cancellationToken);
}