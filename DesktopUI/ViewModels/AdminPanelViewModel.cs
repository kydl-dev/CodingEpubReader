using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;
using Application.UseCases.Cache.ClearCache;
using Application.UseCases.Cache.GetCacheStatistics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Serilog;
using Shared.BackgroundWorkers;
using Shared.BackgroundWorkers.Configuration;
using Shared.BackgroundWorkers.Interfaces;
using Shared.Exceptions;
using Unit = System.Reactive.Unit;

namespace DesktopUI.ViewModels;

public sealed class AdminPanelViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly ICoverImageConfiguration _coverImageConfiguration;
    private readonly ILibraryScanConfiguration _libraryScanConfiguration;
    private readonly ILoggingStatisticsConfiguration _loggingStatisticsConfiguration;
    private readonly ILogStatisticsService _logStatisticsService;
    private readonly IDatabaseMaintenanceConfiguration _maintenanceConfiguration;
    private readonly IMediator _mediator;
    private readonly IReadingSessionTracker _readingSessionTracker;
    private readonly IServiceScopeFactory _scopeFactory;
    private ObservableCollection<ReadingSessionViewModel> _activeReadingSessions;
    private ObservableCollection<string> _allCacheKeys;
    private ObservableCollection<CachePrefixCountItemViewModel> _cacheKeyPrefixCounts;
    private bool _isBusy;
    private DateTime? _lastCoverCacheRunAt;
    private DateTime? _lastDatabaseMaintenanceRunAt;
    private DateTime? _lastLibraryScanRunAt;
    private DateTime? _lastLoggingStatsRunAt;
    private DateTime? _lastReadingSessionRunAt;
    private DateTime? _logEndTime;
    private int _logProcessedFiles;
    private DateTime? _logStartTime;
    private int _logTotalErrors;
    private int _logTotalInformation;
    private int _logTotalWarnings;
    private string _statusMessage;
    private int _thumbnailSizesCount;
    private ObservableCollection<LogTimeSeriesViewModel> _timeSeries;
    private ObservableCollection<ErrorMessageInfoViewModel> _topErrors;

    private int _totalCachedItems;
    private ObservableCollection<string> _watchedFolders;

    public AdminPanelViewModel(
        IMediator mediator,
        IDatabaseMaintenanceConfiguration maintenanceConfiguration,
        ILibraryScanConfiguration libraryScanConfiguration,
        ICoverImageConfiguration coverImageConfiguration,
        ILoggingStatisticsConfiguration loggingStatisticsConfiguration,
        ILogStatisticsService logStatisticsService,
        IReadingSessionTracker readingSessionTracker,
        IServiceScopeFactory scopeFactory)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _maintenanceConfiguration = maintenanceConfiguration ??
                                    throw new ArgumentNullException(nameof(maintenanceConfiguration));
        _libraryScanConfiguration = libraryScanConfiguration ??
                                    throw new ArgumentNullException(nameof(libraryScanConfiguration));
        _coverImageConfiguration =
            coverImageConfiguration ?? throw new ArgumentNullException(nameof(coverImageConfiguration));
        _loggingStatisticsConfiguration = loggingStatisticsConfiguration ??
                                          throw new ArgumentNullException(nameof(loggingStatisticsConfiguration));
        _logStatisticsService = logStatisticsService ?? throw new ArgumentNullException(nameof(logStatisticsService));
        _readingSessionTracker =
            readingSessionTracker ?? throw new ArgumentNullException(nameof(readingSessionTracker));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        _cacheKeyPrefixCounts = [];
        _allCacheKeys = [];
        _watchedFolders = [];
        _topErrors = [];
        _timeSeries = [];
        _activeReadingSessions = [];
        _statusMessage = "Ready";

        RefreshCacheStatsCommand = ReactiveCommand.CreateFromTask(
            RefreshCacheStatsAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        ClearCacheCommand = ReactiveCommand.CreateFromTask(
            ClearCacheAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        RefreshLogStatsCommand = ReactiveCommand.CreateFromTask(
            RefreshLogStatsAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        RunLibraryScanNowCommand = ReactiveCommand.CreateFromTask(
            RunLibraryScanNowAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        RunDatabaseMaintenanceNowCommand = ReactiveCommand.CreateFromTask(
            RunDatabaseMaintenanceNowAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        RunCoverCacheNowCommand = ReactiveCommand.CreateFromTask(
            RunCoverCacheNowAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        RunLoggingStatsNowCommand = ReactiveCommand.CreateFromTask(
            RunLoggingStatsNowAsync,
            outputScheduler: RxApp.MainThreadScheduler);
        RunReadingSessionNowCommand = ReactiveCommand.CreateFromTask(
            RunReadingSessionNowAsync,
            outputScheduler: RxApp.MainThreadScheduler);

        Activator = new ViewModelActivator();

        this.WhenActivated(disposables =>
        {
            RefreshCacheStatsCommand.Execute()
                .Subscribe()
                .DisposeWith(disposables);
            RefreshLogStatsCommand.Execute()
                .Subscribe()
                .DisposeWith(disposables);

            RefreshCacheStatsCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Failed loading cache statistics: {ex.Message}";
                    Log.Error(ex, "Failed loading cache statistics. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            ClearCacheCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Failed clearing cache: {ex.Message}";
                    Log.Error(ex, "Failed clearing cache. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            RefreshLogStatsCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Failed loading logging statistics: {ex.Message}";
                    Log.Error(ex, "Failed loading logging statistics. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            RunLibraryScanNowCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Library scan failed: {ex.Message}";
                    Log.Error(ex, "Library scan run-now failed. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            RunDatabaseMaintenanceNowCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Database maintenance failed: {ex.Message}";
                    Log.Error(ex, "Database maintenance run-now failed. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            RunCoverCacheNowCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Cover cache run failed: {ex.Message}";
                    Log.Error(ex, "Cover cache run-now failed. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            RunLoggingStatsNowCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Logging stats run failed: {ex.Message}";
                    Log.Error(ex, "Logging stats run-now failed. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);

            RunReadingSessionNowCommand.ThrownExceptions
                .Subscribe(ex =>
                {
                    StatusMessage = $"Reading session tracking run failed: {ex.Message}";
                    Log.Error(ex, "Reading session tracking run-now failed. Error: {Error}", ex.FullMessage());
                })
                .DisposeWith(disposables);
        });

        RefreshWorkerConfigurationSnapshot();
    }

    public int TotalCachedItems
    {
        get => _totalCachedItems;
        set => this.RaiseAndSetIfChanged(ref _totalCachedItems, value);
    }

    public ObservableCollection<CachePrefixCountItemViewModel> CacheKeyPrefixCounts
    {
        get => _cacheKeyPrefixCounts;
        set => this.RaiseAndSetIfChanged(ref _cacheKeyPrefixCounts, value);
    }

    public ObservableCollection<string> AllCacheKeys
    {
        get => _allCacheKeys;
        set => this.RaiseAndSetIfChanged(ref _allCacheKeys, value);
    }

    public ObservableCollection<string> WatchedFolders
    {
        get => _watchedFolders;
        set => this.RaiseAndSetIfChanged(ref _watchedFolders, value);
    }

    public int ThumbnailSizesCount
    {
        get => _thumbnailSizesCount;
        set => this.RaiseAndSetIfChanged(ref _thumbnailSizesCount, value);
    }

    public int LogProcessedFiles
    {
        get => _logProcessedFiles;
        set => this.RaiseAndSetIfChanged(ref _logProcessedFiles, value);
    }

    public int LogTotalErrors
    {
        get => _logTotalErrors;
        set => this.RaiseAndSetIfChanged(ref _logTotalErrors, value);
    }

    public int LogTotalWarnings
    {
        get => _logTotalWarnings;
        set => this.RaiseAndSetIfChanged(ref _logTotalWarnings, value);
    }

    public int LogTotalInformation
    {
        get => _logTotalInformation;
        set => this.RaiseAndSetIfChanged(ref _logTotalInformation, value);
    }

    public DateTime? LogStartTime
    {
        get => _logStartTime;
        set => this.RaiseAndSetIfChanged(ref _logStartTime, value);
    }

    public DateTime? LogEndTime
    {
        get => _logEndTime;
        set => this.RaiseAndSetIfChanged(ref _logEndTime, value);
    }

    public ObservableCollection<ErrorMessageInfoViewModel> TopErrors
    {
        get => _topErrors;
        set => this.RaiseAndSetIfChanged(ref _topErrors, value);
    }

    public ObservableCollection<LogTimeSeriesViewModel> TimeSeries
    {
        get => _timeSeries;
        set => this.RaiseAndSetIfChanged(ref _timeSeries, value);
    }

    public ObservableCollection<ReadingSessionViewModel> ActiveReadingSessions
    {
        get => _activeReadingSessions;
        set => this.RaiseAndSetIfChanged(ref _activeReadingSessions, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public DateTime? LastLibraryScanRunAt
    {
        get => _lastLibraryScanRunAt;
        set => this.RaiseAndSetIfChanged(ref _lastLibraryScanRunAt, value);
    }

    public DateTime? LastDatabaseMaintenanceRunAt
    {
        get => _lastDatabaseMaintenanceRunAt;
        set => this.RaiseAndSetIfChanged(ref _lastDatabaseMaintenanceRunAt, value);
    }

    public DateTime? LastCoverCacheRunAt
    {
        get => _lastCoverCacheRunAt;
        set => this.RaiseAndSetIfChanged(ref _lastCoverCacheRunAt, value);
    }

    public DateTime? LastLoggingStatsRunAt
    {
        get => _lastLoggingStatsRunAt;
        set => this.RaiseAndSetIfChanged(ref _lastLoggingStatsRunAt, value);
    }

    public DateTime? LastReadingSessionRunAt
    {
        get => _lastReadingSessionRunAt;
        set => this.RaiseAndSetIfChanged(ref _lastReadingSessionRunAt, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    public bool MaintenanceEnabled => _maintenanceConfiguration.IsEnabled;
    public string MaintenanceInterval => _maintenanceConfiguration.MaintenanceInterval.ToString();
    public bool VacuumEnabled => _maintenanceConfiguration.EnableVacuum;
    public bool OrphanCleanupEnabled => _maintenanceConfiguration.EnableOrphanedRecordCleanup;
    public bool CompressionEnabled => _maintenanceConfiguration.EnableDataCompression;
    public int CompressionAgeDays => _maintenanceConfiguration.DataCompressionAgeDays;
    public bool StatisticsUpdateEnabled => _maintenanceConfiguration.EnableStatisticsUpdate;
    public bool TempCleanupEnabled => _maintenanceConfiguration.EnableTempFileCleanup;
    public int TempCleanupAgeDays => _maintenanceConfiguration.TempFileAgeDays;

    public bool LibraryScanEnabled => _libraryScanConfiguration.IsEnabled;
    public string LibraryScanInterval => _libraryScanConfiguration.ScanInterval.ToString();
    public bool LibraryScanOnStartup => _libraryScanConfiguration.ScanOnStartup;

    public bool CoverCacheEnabled => _coverImageConfiguration.IsEnabled;
    public string CoverCacheInterval => _coverImageConfiguration.CacheUpdateInterval.ToString();
    public bool CoverCacheOnStartup => _coverImageConfiguration.GenerateOnStartup;
    public bool CoverCacheCleanupEnabled => _coverImageConfiguration.EnableCacheCleanup;
    public int CoverCacheMaxAgeDays => _coverImageConfiguration.CacheMaxAgeDays;

    public bool LoggingStatsEnabled => _loggingStatisticsConfiguration.IsEnabled;
    public string LoggingStatsInterval => _loggingStatisticsConfiguration.AggregationInterval.ToString();
    public int LoggingStatsTopErrorsCount => _loggingStatisticsConfiguration.TopErrorsCount;
    public int LoggingStatsRetentionDays => _loggingStatisticsConfiguration.LogRetentionDays;

    public ReactiveCommand<Unit, Unit> RefreshCacheStatsCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCacheCommand { get; }
    public ReactiveCommand<Unit, Unit> RefreshLogStatsCommand { get; }
    public ReactiveCommand<Unit, Unit> RunLibraryScanNowCommand { get; }
    public ReactiveCommand<Unit, Unit> RunDatabaseMaintenanceNowCommand { get; }
    public ReactiveCommand<Unit, Unit> RunCoverCacheNowCommand { get; }
    public ReactiveCommand<Unit, Unit> RunLoggingStatsNowCommand { get; }
    public ReactiveCommand<Unit, Unit> RunReadingSessionNowCommand { get; }

    public ViewModelActivator Activator { get; }

    private async Task RefreshCacheStatsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading cache statistics...";

            var stats = await _mediator.Send(new GetCacheStatisticsQuery());

            TotalCachedItems = stats.TotalCachedItems;

            CacheKeyPrefixCounts.Clear();
            foreach (var pair in stats.KeysByPrefix.OrderByDescending(kvp => kvp.Value).ThenBy(kvp => kvp.Key))
                CacheKeyPrefixCounts.Add(new CachePrefixCountItemViewModel
                {
                    Prefix = pair.Key,
                    Count = pair.Value
                });

            AllCacheKeys.Clear();
            foreach (var key in stats.AllKeys.OrderBy(k => k)) AllCacheKeys.Add(key);

            StatusMessage = $"Cache statistics updated ({TotalCachedItems} items).";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed loading cache statistics: {ex.Message}";
            Log.Error(ex, "Failed loading cache statistics. Error: {Error}", ex.FullMessage());
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ClearCacheAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Clearing cache...";

            await _mediator.Send(new ClearCacheCommand());
            await RefreshCacheStatsAsync();

            StatusMessage = "Cache cleared successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed clearing cache: {ex.Message}";
            Log.Error(ex, "Failed clearing cache. Error: {Error}", ex.FullMessage());
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshLogStatsAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Loading logging statistics...";

            var stats = await _logStatisticsService.GetCachedStatisticsAsync();
            if (stats == null)
            {
                LogProcessedFiles = 0;
                LogTotalErrors = 0;
                LogTotalWarnings = 0;
                LogTotalInformation = 0;
                LogStartTime = null;
                LogEndTime = null;
                TopErrors.Clear();
                TimeSeries.Clear();
                StatusMessage = "No cached logging statistics yet (worker may not have run).";
                return;
            }

            LogProcessedFiles = stats.ProcessedFiles;
            LogTotalErrors = stats.TotalErrors;
            LogTotalWarnings = stats.TotalWarnings;
            LogTotalInformation = stats.TotalInformation;
            LogStartTime = stats.StartTime;
            LogEndTime = stats.EndTime;

            TopErrors.Clear();
            foreach (var top in stats.TopErrors.Values.OrderByDescending(v => v.Count))
                TopErrors.Add(new ErrorMessageInfoViewModel
                {
                    ErrorType = top.ErrorType,
                    Message = top.Message,
                    Count = top.Count,
                    LastOccurrence = top.LastOccurrence
                });

            TimeSeries.Clear();
            foreach (var point in stats.TimeSeriesData.Values.OrderByDescending(v => v.Timestamp).Take(48))
                TimeSeries.Add(new LogTimeSeriesViewModel
                {
                    Timestamp = point.Timestamp,
                    ErrorCount = point.ErrorCount,
                    WarningCount = point.WarningCount,
                    InfoCount = point.InfoCount
                });

            StatusMessage = $"Logging statistics updated ({LogProcessedFiles} files processed).";
            RefreshActiveReadingSessions();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed loading logging statistics: {ex.Message}";
            Log.Error(ex, "Failed loading logging statistics. Error: {Error}", ex.FullMessage());
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshWorkerConfigurationSnapshot()
    {
        WatchedFolders.Clear();
        foreach (var folder in _libraryScanConfiguration.GetWatchedFolders().Where(f => !string.IsNullOrWhiteSpace(f)))
            WatchedFolders.Add(folder!);

        ThumbnailSizesCount = _coverImageConfiguration.GetThumbnailSizes().Count();
        RefreshActiveReadingSessions();
    }

    private async Task RunLibraryScanNowAsync()
    {
        await RunWorkerNowAsync<LibraryScanningWorker>("Library Scanning Worker");
        LastLibraryScanRunAt = DateTime.UtcNow;
    }

    private async Task RunDatabaseMaintenanceNowAsync()
    {
        await RunWorkerNowAsync<DatabaseMaintenanceWorker>("Database Maintenance Worker");
        LastDatabaseMaintenanceRunAt = DateTime.UtcNow;
    }

    private async Task RunCoverCacheNowAsync()
    {
        await RunWorkerNowAsync<CoverImageCacheWorker>("Cover Image Cache Worker");
        LastCoverCacheRunAt = DateTime.UtcNow;
    }

    private async Task RunLoggingStatsNowAsync()
    {
        await RunWorkerNowAsync<LoggingStatisticsWorker>("Logging Statistics Worker");
        LastLoggingStatsRunAt = DateTime.UtcNow;
        await RefreshLogStatsAsync();
    }

    private async Task RunReadingSessionNowAsync()
    {
        await RunWorkerNowAsync<ReadingSessionTrackingWorker>("Reading Session Tracking Worker");
        LastReadingSessionRunAt = DateTime.UtcNow;
        RefreshActiveReadingSessions();
    }

    private async Task RunWorkerNowAsync<TWorker>(string workerDisplayName)
        where TWorker : class
    {
        try
        {
            IsBusy = true;
            StatusMessage = $"Running {workerDisplayName}...";

            using var scope = _scopeFactory.CreateScope();
            var worker = scope.ServiceProvider.GetRequiredService<TWorker>();
            if (worker is not BackgroundWorkerBase<TWorker> typedWorker)
                throw new InvalidOperationException($"{typeof(TWorker).Name} is not a BackgroundWorkerBase.");

            await typedWorker.ExecuteAsync(CancellationToken.None);
            StatusMessage = $"{workerDisplayName} completed.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"{workerDisplayName} failed: {ex.Message}";
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void RefreshActiveReadingSessions()
    {
        ActiveReadingSessions.Clear();
        foreach (var session in _readingSessionTracker.GetActiveSessions())
            ActiveReadingSessions.Add(new ReadingSessionViewModel
            {
                BookId = session.BookId,
                CurrentChapterId = session.CurrentChapterId,
                CurrentPosition = session.CurrentPosition,
                StartTime = session.StartTime,
                LastActivityTime = session.LastActivityTime,
                Duration = session.ReadingDuration
            });
    }
}

public sealed class CachePrefixCountItemViewModel
{
    public string Prefix { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ErrorMessageInfoViewModel
{
    public string ErrorType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime LastOccurrence { get; set; }
}

public sealed class LogTimeSeriesViewModel
{
    public DateTime Timestamp { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
}

public sealed class ReadingSessionViewModel
{
    public Guid BookId { get; set; }
    public string CurrentChapterId { get; set; } = string.Empty;
    public int CurrentPosition { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime LastActivityTime { get; set; }
    public TimeSpan Duration { get; set; }
}