using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Logs;
using Shared.Exceptions;

namespace Shared.BackgroundWorkers;

public abstract class BackgroundWorkerBase<TWorker>(
    ILogger<TWorker> logger)
    : IBackgroundWorker
{
    protected readonly ILogger<TWorker> Logger = logger
                                                 ?? throw new ArgumentNullException(nameof(logger));

    private Task? _executingTask;
    private CancellationTokenSource? _stoppingCts;
    protected abstract TimeSpan ExecutionInterval { get; }
    protected virtual bool RunImmediatelyOnStartup => false;

    public abstract string WorkerName { get; }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        BaseWorkerLogging<TWorker>.Starting(Logger, WorkerName);

        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteLoopAsync(_stoppingCts.Token);

        return _executingTask.IsCompleted
            ? _executingTask
            : Task.CompletedTask;
    }

    public virtual async Task StopAsync(CancellationToken cancellationToken)
    {
        BaseWorkerLogging<TWorker>.Stopping(Logger, WorkerName);

        if (_executingTask == null)
            return;

        try
        {
            _stoppingCts?.Cancel();
        }
        finally
        {
            await Task.WhenAny(
                _executingTask,
                Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    public abstract Task ExecuteAsync(CancellationToken cancellationToken);

    private async Task ExecuteLoopAsync(CancellationToken cancellationToken)
    {
        BaseWorkerLogging<TWorker>.Started(Logger, WorkerName, ExecutionInterval);

        try
        {
            if (RunImmediatelyOnStartup)
                await ExecuteWithErrorHandlingAsync(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(ExecutionInterval, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                await ExecuteWithErrorHandlingAsync(cancellationToken);
            }
        }
        finally
        {
            BaseWorkerLogging<TWorker>.Stopped(Logger, WorkerName);
        }
    }

    private async Task ExecuteWithErrorHandlingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ExecuteAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(
                    ex,
                    "Error executing background worker {WorkerName}. Error: {Error}",
                    WorkerName,
                    ex.FullMessage());
        }
    }
}