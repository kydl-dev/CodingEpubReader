using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;

namespace Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _libraryPath;
    private readonly ILogger<FileStorageService> _logger;

    public FileStorageService(ILogger<FileStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Set up library path in application data directory
        var appDataPath = GetAppDataPath();
        _libraryPath = Path.Combine(appDataPath, "Library");

        // Ensure library directory exists
        if (Directory.Exists(_libraryPath)) return;
        Directory.CreateDirectory(_libraryPath);
        _logger.LogInformation("Created library directory at: {LibraryPath}", _libraryPath);
    }

    public bool FileExists(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Path is null or empty");
            return false;
        }

        return File.Exists(path);
    }

    public IEnumerable<string> GetEpubFilesInFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            _logger.LogWarning("Folder path is null or empty");
            return [];
        }

        if (!Directory.Exists(folderPath))
        {
            _logger.LogWarning("Folder does not exist: {FolderPath}", folderPath);
            return [];
        }

        try
        {
            return Directory.GetFiles(folderPath, "*.epub", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get epub files from folder: {FolderPath}. Error: {Error}", folderPath,
                ex.FullMessage());
            return [];
        }
    }

    public async Task<string> CopyToLibraryAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path cannot be empty.", nameof(sourcePath));

        if (!File.Exists(sourcePath))
            throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);

        var fileName = Path.GetFileName(sourcePath);
        var destinationPath = Path.Combine(_libraryPath, fileName);

        // If file already exists, append a unique identifier
        if (File.Exists(destinationPath))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            fileName = $"{fileNameWithoutExtension}_{uniqueId}{extension}";
            destinationPath = Path.Combine(_libraryPath, fileName);
        }

        try
        {
            _logger.LogInformation("Copying file from {Source} to {Destination}", sourcePath, destinationPath);

            using var sourceStream =
                new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
            using var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 81920, true);

            await sourceStream.CopyToAsync(destinationStream, cancellationToken);

            _logger.LogInformation("Successfully copied file to library");

            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file to library. Error: {Error}", ex.FullMessage());
            throw;
        }
    }

    public async Task DeleteFromLibraryAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            _logger.LogWarning("Attempted to delete file with empty path");
            return;
        }

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("File does not exist, nothing to delete: {FilePath}", filePath);
            return;
        }

        // Ensure the file is within the library directory for safety
        var normalizedPath = Path.GetFullPath(filePath);
        var normalizedLibraryPath = Path.GetFullPath(_libraryPath);

        if (!normalizedPath.StartsWith(normalizedLibraryPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted to delete file outside library directory: {FilePath}", filePath);
            throw new InvalidOperationException("Cannot delete files outside the library directory.");
        }

        try
        {
            _logger.LogInformation("Deleting file from library: {FilePath}", filePath);
            await Task.Run(() => File.Delete(filePath), cancellationToken);
            _logger.LogInformation("Successfully deleted file from library");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from library: {FilePath}. Error: {Error}", filePath,
                ex.FullMessage());
            throw;
        }
    }

    public string GetAppDataPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var epubReaderPath = Path.Combine(appDataPath, "EpubReader");

        if (!Directory.Exists(epubReaderPath))
        {
            Directory.CreateDirectory(epubReaderPath);
            _logger.LogInformation("Created application data directory at: {AppDataPath}", epubReaderPath);
        }

        return epubReaderPath;
    }
}