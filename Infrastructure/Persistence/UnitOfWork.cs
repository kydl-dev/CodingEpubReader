using Domain.Repositories;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public class UnitOfWork(EpubReaderDbContext context) : IUnitOfWork
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private IBookmarkRepository? _bookmarks;

    private IBookRepository? _books;
    private IDbContextTransaction? _currentTransaction;
    private IHighlightRepository? _highlights;
    private IReadingProgressRepository? _readingProgress;

    public IBookRepository Books =>
        _books ??= new BookRepository(_context);

    public IBookmarkRepository Bookmarks =>
        _bookmarks ??= new BookmarkRepository(_context);

    public IHighlightRepository Highlights =>
        _highlights ??= new HighlightRepository(_context);

    public IReadingProgressRepository ReadingProgress =>
        _readingProgress ??= new ReadingProgressRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) throw new InvalidOperationException("A transaction is already in progress.");

        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null) throw new InvalidOperationException("No transaction in progress.");

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null) throw new InvalidOperationException("No transaction in progress.");

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}