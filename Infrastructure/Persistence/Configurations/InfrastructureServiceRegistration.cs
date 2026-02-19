using System.Data;
using System.Reflection;
using Application.Interfaces;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Infrastructure.Configuration.WorkersConfigurations;
using Infrastructure.EpubParsing;
using Infrastructure.Persistence.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Adapters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.BackgroundWorkers;
using Shared.BackgroundWorkers.Configuration;
using Shared.BackgroundWorkers.Interfaces;
using Shared.BackgroundWorkers.Utils;
using SQLitePCL;

namespace Infrastructure.Persistence.Configurations;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Initialize SQLite provider
        Batteries.Init();

        // Database configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? GetDefaultConnectionString();

        services.AddDbContext<EpubReaderDbContext>(options =>
        {
            options.UseSqlite(connectionString, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly(typeof(EpubReaderDbContext).Assembly.FullName);
                sqliteOptions.CommandTimeout(30);
            });

            // Enable detailed errors in development
#if DEBUG
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
#endif
        });

        // Repository registrations
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IBookmarkRepository, BookmarkRepository>();
        services.AddScoped<IHighlightRepository, HighlightRepository>();
        services.AddScoped<IReadingProgressRepository, ReadingProgressRepository>();
        services.AddScoped<IReadingHistoryRepository, ReadingHistoryRepository>();
        services.AddScoped<ISavedCssStyleRepository, SavedCssStyleRepository>();
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<ILibraryIndexRepository, LibraryIndexRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();

        // Unit of Work registration
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Service registrations
        services.AddScoped<IEpubParser, EpubParserService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ILibraryService, LibraryService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddScoped<IBookContentService, BookContentService>();

        // Background worker service registrations
        services.AddSingleton<IReadingSessionTracker, ReadingSessionTracker>();
        services.AddScoped<IReadingHistoryWorker, ReadingHistoryWorker>();
        services.AddScoped<IDatabaseMaintenanceService, DatabaseMaintenanceService>();
        services.AddSingleton<ICoverImageCacheWorker, CoverImageCacheService>();
        services.AddSingleton<ILogStatisticsService, LogStatisticsService>();
        services.AddSingleton<ILogFileProvider, LogFileProvider>();

        // Service adapters for background workers
        services.AddScoped<ILibraryScanningService, LibraryScanningServiceAdapter>();
        services.AddScoped<IBookRepositoryWorker, BookRepositoryWorkerAdapter>();

        // Worker configuration registrations
        services.AddSingleton<ILibraryScanConfiguration, LibraryScanConfiguration>();
        services.AddSingleton<IDatabaseMaintenanceConfiguration, DatabaseMaintenanceConfiguration>();
        services.AddSingleton<ICoverImageConfiguration, CoverImageConfiguration>();
        services.AddSingleton<ILoggingStatisticsConfiguration, LoggingStatisticsConfiguration>();

        // Worker concrete registrations (for admin "Run Now" execution)
        services.AddTransient<CoverImageCacheWorker>();
        services.AddTransient<DatabaseMaintenanceWorker>();
        services.AddTransient<LibraryScanningWorker>();
        services.AddTransient<LoggingStatisticsWorker>();
        services.AddTransient<ReadingSessionTrackingWorker>();

        // Cache service
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();

        // Register Background Workers as Hosted Services
        services.AddHostedService<CoverImageCacheWorker>();
        services.AddHostedService<DatabaseMaintenanceWorker>();
        services.AddHostedService<LibraryScanningWorker>();
        services.AddHostedService<LoggingStatisticsWorker>();
        services.AddHostedService<ReadingSessionTrackingWorker>();

        return services;
    }

    /// <summary>
    ///     Ensures the database is created and migrations are applied
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EpubReaderDbContext>();

        // Apply any pending migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
            await context.Database.MigrateAsync();
        else
            // Create database if it doesn't exist (first run)
            await context.Database.EnsureCreatedAsync();

        // Enable WAL (Write-Ahead Logging) journal mode.
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
        await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=5000;");

        // Forward-compat schema patch for older local DBs that were created
        // before ReadingHistory metadata snapshot columns were introduced.
        await EnsureReadingHistorySnapshotColumnsAsync(context);
    }

    /// <summary>
    ///     Gets the default SQLite connection string
    ///     DEBUG: Uses local project folder
    ///     RELEASE: Uses LocalApplicationData folder
    /// </summary>
    private static string GetDefaultConnectionString()
    {
#if DEBUG
        // DEBUG: Use Infrastructure/Persistence/Migrations folder
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        if (assemblyLocation == null) throw new InvalidOperationException("Could not determine assembly location");

        var projectRoot = Directory.GetParent(assemblyLocation)?.Parent?.Parent?.FullName;

        if (projectRoot == null) throw new InvalidOperationException("Could not determine Infrastructure project root");

        var migrationsFolder = Path.Combine(projectRoot, "Persistence", "Migrations");

        if (!Directory.Exists(migrationsFolder)) Directory.CreateDirectory(migrationsFolder);

        var databasePath = Path.Combine(migrationsFolder, "epubreader_dev.db");

        Console.WriteLine($"[DEBUG] Using database at: {databasePath}");

        return $"Data Source={databasePath};";
#else
        // RELEASE: Use LocalApplicationData for production
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var epubReaderPath = Path.Combine(appDataPath, "EpubReader");

        if (!Directory.Exists(epubReaderPath))
        {
            Directory.CreateDirectory(epubReaderPath);
        }

        var databasePath = Path.Combine(epubReaderPath, "epubreader.db");

        Console.WriteLine($"[RELEASE] Using database at: {databasePath}");

        return $"Data Source={databasePath};";
#endif
    }

    /// <summary>
    ///     Seeds the database with initial data if needed
    /// </summary>
    public static async Task SeedDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EpubReaderDbContext>();

        var styles = await context.SavedCssStyles.ToListAsync();
        var hasChanges = false;

        if (!styles.Any(s => string.Equals(s.Name, "Light", StringComparison.OrdinalIgnoreCase)))
        {
            context.SavedCssStyles.Add(SavedCssStyle.Create("Light", CssStyle.Default));
            hasChanges = true;
        }

        if (!styles.Any(s => string.Equals(s.Name, "Dracula", StringComparison.OrdinalIgnoreCase)))
        {
            context.SavedCssStyles.Add(SavedCssStyle.Create("Dracula", CssStyle.Dracula));
            hasChanges = true;
        }

        if (!styles.Any(s => string.Equals(s.Name, "Sepia", StringComparison.OrdinalIgnoreCase)))
        {
            context.SavedCssStyles.Add(SavedCssStyle.Create("Sepia", CssStyle.Sepia));
            hasChanges = true;
        }

        if (hasChanges) await context.SaveChangesAsync();

        styles = await context.SavedCssStyles.ToListAsync();
        var defaultStyle = styles.FirstOrDefault(s => s.IsDefault);

        if (defaultStyle == null)
        {
            var dracula =
                styles.FirstOrDefault(s => string.Equals(s.Name, "Dracula", StringComparison.OrdinalIgnoreCase));
            if (dracula != null)
            {
                dracula.SetAsDefault();
                await context.SaveChangesAsync();
            }
        }
    }

    private static async Task EnsureReadingHistorySnapshotColumnsAsync(EpubReaderDbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync();

        try
        {
            var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA table_info('ReadingHistories');";
                using var reader = await pragma.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    if (reader["name"] is string name && !string.IsNullOrWhiteSpace(name))
                        existingColumns.Add(name);
            }

            if (existingColumns.Count == 0) return;

            if (!existingColumns.Contains("BookTitle"))
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"ReadingHistories\" ADD COLUMN \"BookTitle\" TEXT NOT NULL DEFAULT '';");

            if (!existingColumns.Contains("BookAuthor"))
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"ReadingHistories\" ADD COLUMN \"BookAuthor\" TEXT NOT NULL DEFAULT '';");

            if (!existingColumns.Contains("BookIsbn"))
                await context.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE \"ReadingHistories\" ADD COLUMN \"BookIsbn\" TEXT NULL;");
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }
}