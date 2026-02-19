namespace Domain.Entities;

/// <summary>
///     Represents the library catalog with metadata about the indexed books collection.
///     This provides a high-level view of the entire library.
/// </summary>
public class Library
{
    // EF Core / serialization constructor
    private Library()
    {
    }

    private Library(
        Guid id,
        string name,
        string description,
        DateTime createdAt)
    {
        Id = id;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        LastUpdatedAt = createdAt;
        TotalBooks = 0;
    }

    public Guid Id { get; private set; }

    /// <summary>
    ///     The name of the library (e.g., "My Books", "Work Library")
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    ///     Optional description of the library
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    ///     When the library was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    ///     When the library was last updated (book added/removed)
    /// </summary>
    public DateTime LastUpdatedAt { get; private set; }

    /// <summary>
    ///     Total number of books in the library
    /// </summary>
    public int TotalBooks { get; private set; }

    /// <summary>
    ///     Optional folder path where books are stored
    /// </summary>
    public string? StoragePath { get; private set; }

    public static Library Create(string name, string description = "", string? storagePath = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Library name cannot be empty.", nameof(name));

        var library = new Library(
            Guid.NewGuid(),
            name,
            description,
            DateTime.UtcNow);

        library.StoragePath = storagePath;

        return library;
    }

    /// <summary>
    ///     Updates the library name
    /// </summary>
    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Library name cannot be empty.", nameof(newName));

        Name = newName;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Updates the library description
    /// </summary>
    public void UpdateDescription(string? newDescription)
    {
        Description = newDescription ?? string.Empty;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Updates the storage path
    /// </summary>
    public void UpdateStoragePath(string? newPath)
    {
        StoragePath = newPath;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Increments the book count when a book is added
    /// </summary>
    public void IncrementBookCount()
    {
        TotalBooks++;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Decrements the book count when a book is removed
    /// </summary>
    public void DecrementBookCount()
    {
        if (TotalBooks > 0) TotalBooks--;
        LastUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    ///     Syncs the book count with the actual number in the database
    /// </summary>
    public void SyncBookCount(int actualCount)
    {
        if (actualCount < 0)
            throw new ArgumentException("Book count cannot be negative.", nameof(actualCount));

        TotalBooks = actualCount;
        LastUpdatedAt = DateTime.UtcNow;
    }
}