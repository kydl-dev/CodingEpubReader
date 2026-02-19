using Microsoft.Extensions.Logging;

namespace Shared.BackgroundWorkers.Logs;

internal static partial class BaseWorkerLogging<TWorker>
{
    [LoggerMessage(
        EventId = EventIdRange.BackgroundWorkerBase + 0,
        Level = LogLevel.Information,
        Message = "Starting background worker: {WorkerName}")]
    public static partial void Starting(
        ILogger<TWorker> logger,
        string workerName);

    [LoggerMessage(
        EventId = EventIdRange.BackgroundWorkerBase + 1,
        Level = LogLevel.Information,
        Message = "Stopping background worker: {WorkerName}")]
    public static partial void Stopping(
        ILogger<TWorker> logger,
        string workerName);

    [LoggerMessage(
        EventId = EventIdRange.BackgroundWorkerBase + 2,
        Level = LogLevel.Information,
        Message = "Background worker {WorkerName} started with interval: {Interval}")]
    public static partial void Started(
        ILogger<TWorker> logger,
        string workerName,
        TimeSpan interval);

    [LoggerMessage(
        EventId = EventIdRange.BackgroundWorkerBase + 3,
        Level = LogLevel.Information,
        Message = "Background worker {WorkerName} stopped")]
    public static partial void Stopped(
        ILogger<TWorker> logger,
        string workerName);
}