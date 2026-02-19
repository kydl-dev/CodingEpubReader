using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.BackgroundWorkers.Interfaces;
using Shared.Exceptions;

namespace Infrastructure.Services;

/// <summary>
///     Service for performing database maintenance operations.
/// </summary>
public class DatabaseMaintenanceService(
    EpubReaderDbContext dbContext,
    IBookRepository bookRepository,
    ILogger<DatabaseMaintenanceService> logger)
    : IDatabaseMaintenanceService
{
    private readonly IBookRepository _bookRepository =
        bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));

    private readonly EpubReaderDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    private readonly ILogger<DatabaseMaintenanceService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly string _tempFilesDirectory = Path.Combine(Path.GetTempPath(), "EpubReader");

    public async Task VacuumDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database vacuum operation");
            await _dbContext.Database.ExecuteSqlRawAsync("VACUUM;", cancellationToken);
            _logger.LogInformation("Database vacuum completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database vacuum. Error: {Error}", ex.FullMessage());
            throw;
        }
    }

    public async Task<int> CleanupOrphanedRecordsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of orphaned records");
            var deletedCount = 0;

            var validBookIds = await _dbContext.Books
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            // Clean up orphaned bookmarks.
            var orphanedBookmarks = await _dbContext.Bookmarks
                .Where(b => !validBookIds.Contains(b.BookId))
                .ToListAsync(cancellationToken);

            if (orphanedBookmarks.Count != 0)
            {
                _dbContext.Bookmarks.RemoveRange(orphanedBookmarks);
                deletedCount += orphanedBookmarks.Count;
                _logger.LogDebug("Found {Count} orphaned bookmarks", orphanedBookmarks.Count);
            }

            // Clean up orphaned highlights.
            var orphanedHighlights = await _dbContext.Highlights
                .Where(h => !validBookIds.Contains(h.BookId))
                .ToListAsync(cancellationToken);

            if (orphanedHighlights.Count != 0)
            {
                _dbContext.Highlights.RemoveRange(orphanedHighlights);
                deletedCount += orphanedHighlights.Count;
                _logger.LogDebug("Found {Count} orphaned highlights", orphanedHighlights.Count);
            }

            // Clean up orphaned reading positions.
            var orphanedPositions = await _dbContext.ReadingPositions
                .Where(p => !validBookIds.Contains(p.BookId))
                .ToListAsync(cancellationToken);

            if (orphanedPositions.Count != 0)
            {
                _dbContext.ReadingPositions.RemoveRange(orphanedPositions);
                deletedCount += orphanedPositions.Count;
                _logger.LogDebug("Found {Count} orphaned reading positions", orphanedPositions.Count);
            }

            // NOTE: ReadingHistory records with a null BookId are intentionally kept.
            // They represent history for books that were deleted — the title/author/ISBN
            // snapshot on the record keeps them meaningful for future re-imports.

            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} orphaned records", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during orphaned records cleanup. Error: {Error}", ex.FullMessage());
            throw;
        }
    }

    public async Task<int> CompressOldDataAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting compression of old data (older than {Date})", olderThan);

            // Placeholder: In a real implementation you might aggregate detailed reading
            // sessions into summary records, or archive very old history entries.
            var oldHistoryEntries = await _dbContext.ReadingHistories
                .Where(h => h.LastReadAt < olderThan)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Found {Count} old reading history records that could be compressed",
                oldHistoryEntries.Count);

            return oldHistoryEntries.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data compression. Error: {Error}", ex.FullMessage());
            throw;
        }
    }

    public async Task UpdateStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting database statistics update");
            await _dbContext.Database.ExecuteSqlRawAsync("ANALYZE;", cancellationToken);
            _logger.LogInformation("Database statistics updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during statistics update. Error: {Error}", ex.FullMessage());
            throw;
        }
    }

    public Task<int> CleanupTempFilesAsync(int olderThanDays, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of temporary files older than {Days} days", olderThanDays);
            var deletedCount = 0;

            if (!Directory.Exists(_tempFilesDirectory))
            {
                _logger.LogDebug("Temp directory does not exist: {Directory}", _tempFilesDirectory);
                return Task.FromResult(0);
            }

            var threshold = DateTime.UtcNow.AddDays(-olderThanDays);
            var files = Directory.GetFiles(_tempFilesDirectory, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTimeUtc < threshold)
                    {
                        File.Delete(file);
                        deletedCount++;
                        _logger.LogDebug("Deleted old temp file: {File}", file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp file: {File}", file);
                }
            }

            var directories = Directory.GetDirectories(_tempFilesDirectory, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length);

            foreach (var directory in directories)
                try
                {
                    if (Directory.EnumerateFileSystemEntries(directory).Any()) continue;
                    Directory.Delete(directory);
                    _logger.LogDebug("Deleted empty temp directory: {Directory}", directory);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp directory: {Directory}", directory);
                }

            _logger.LogInformation("Cleaned up {Count} old temporary files", deletedCount);
            return Task.FromResult(deletedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temp files cleanup. Error: {Error}", ex.FullMessage());
            throw;
        }
    }
}