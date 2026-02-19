using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SQLitePCL;

namespace Infrastructure.Persistence;

/// <summary>
///     Factory for creating DbContext instances at design time (for migrations, etc.)
///     - DEBUG: Creates database in Infrastructure/Persistence/Migrations folder
///     - RELEASE: Creates database in user's LocalApplicationData folder
/// </summary>
public class EpubReaderDbContextFactory : IDesignTimeDbContextFactory<EpubReaderDbContext>
{
    public EpubReaderDbContext CreateDbContext(string[] args)
    {
        // Initialize SQLite provider
        Batteries.Init();
        var optionsBuilder = new DbContextOptionsBuilder<EpubReaderDbContext>();

        var databasePath = GetDatabasePath();

        Console.WriteLine($"[{GetBuildConfiguration()}] Creating database at: {databasePath}");

        optionsBuilder.UseSqlite($"Data Source={databasePath};");

        return new EpubReaderDbContext(optionsBuilder.Options);
    }

    private static string GetDatabasePath([CallerFilePath] string sourceFilePath = "")
    {
#if DEBUG
        // DEBUG: Use Infrastructure/Persistence/Migrations folder
        // sourceFilePath will be the actual .cs file location
        var sourceDirectory = Path.GetDirectoryName(sourceFilePath);

        // We're in Infrastructure/Persistence, so go to Persistence/Migrations
        Debug.Assert(sourceDirectory != null, nameof(sourceDirectory) + " != null");
        var migrationsFolder = Path.Combine(sourceDirectory, "Migrations");

        // Ensure directory exists
        if (!Directory.Exists(migrationsFolder)) Directory.CreateDirectory(migrationsFolder);

        return Path.Combine(migrationsFolder, "epubreader_dev.db");
#else
        // RELEASE: Use LocalApplicationData folder
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var epubReaderPath = Path.Combine(appDataPath, "EpubReader");
        
        // Ensure directory exists
        if (!Directory.Exists(epubReaderPath))
        {
            Directory.CreateDirectory(epubReaderPath);
        }
        
        return Path.Combine(epubReaderPath, "epubreader.db");
#endif
    }

    private static string GetBuildConfiguration()
    {
#if DEBUG
        return "DEBUG";
#else
        return "RELEASE";
#endif
    }
}