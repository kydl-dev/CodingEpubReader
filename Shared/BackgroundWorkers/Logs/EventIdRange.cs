namespace Shared.BackgroundWorkers.Logs;

public static class EventIdRange
{
    // 1000–1999 → Background workers lifecycle
    public const int BackgroundWorkerBase = 1000;

    // 2000–2999 → LoggingStatisticsWorker
    public const int LoggingStatisticsWorker = 2000;

    // 3000–3999 → LibraryScanWorker
    public const int LibraryScanWorker = 3000;

    // 4000–4999 → DatabaseMaintenanceWorker
    public const int DatabaseMaintenanceWorker = 4000;

    // 5000-5999 → CoverImageCacheWorker
    public const int CoverImageCacheWorker = 5000;

    // 6000-6999 → ReadingSessionTrackingWorker
    public const int ReadingSessionTrackingWorker = 6000;
}