namespace Application.Interfaces;

/// <summary>
///     Abstracts file system access so that use case handlers never touch
///     <c>System.IO</c> directly, keeping them testable.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Returns true if a file exists at the given path.</summary>
    bool FileExists(string path);

    /// <summary>Returns all epub file paths found directly inside <paramref name="folderPath" />.</summary>
    IEnumerable<string> GetEpubFilesInFolder(string folderPath);

    /// <summary>
    ///     Copies a book file into the application's managed library storage directory
    ///     and returns the new absolute path.
    /// </summary>
    Task<string> CopyToLibraryAsync(string sourcePath, CancellationToken cancellationToken = default);

    /// <summary>Deletes a book file from the managed library storage directory.</summary>
    Task DeleteFromLibraryAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>Returns the absolute path to the application data directory.</summary>
    string GetAppDataPath();
}