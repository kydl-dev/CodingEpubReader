using Domain.Repositories;

namespace Infrastructure.Persistence;

/// <summary>
///     Unit of Work pattern for managing transactions across multiple repositories
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IBookRepository Books { get; }
    IBookmarkRepository Bookmarks { get; }
    IHighlightRepository Highlights { get; }
    IReadingProgressRepository ReadingProgress { get; }

    /// <summary>
    ///     Saves all changes made in this unit of work
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Begins a new database transaction
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Commits the current transaction
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Rolls back the current transaction
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}