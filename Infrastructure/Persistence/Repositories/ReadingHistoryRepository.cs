using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ReadingHistoryRepository(EpubReaderDbContext context) : IReadingHistoryRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<ReadingHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingHistory>()
            .Include(rh => rh.Book)
            .FirstOrDefaultAsync(rh => rh.Id == id, cancellationToken);
    }

    public async Task<ReadingHistory?> GetByBookIdAsync(BookId bookId, CancellationToken cancellationToken = default)
    {
        if (bookId == null)
            throw new ArgumentNullException(nameof(bookId));

        return await _context.Set<ReadingHistory>()
            .Include(rh => rh.Book)
            .FirstOrDefaultAsync(rh => rh.BookId == bookId, cancellationToken);
    }

    public async Task<IEnumerable<ReadingHistory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingHistory>()
            .Include(rh => rh.Book)
            .OrderByDescending(rh => rh.LastReadAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ReadingHistory>> GetRecentAsync(int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<ReadingHistory>()
            .Include(rh => rh.Book)
            .OrderByDescending(rh => rh.LastReadAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReadingHistory> AddAsync(ReadingHistory history, CancellationToken cancellationToken = default)
    {
        if (history == null)
            throw new ArgumentNullException(nameof(history));

        await _context.Set<ReadingHistory>().AddAsync(history, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return history;
    }

    public async Task UpdateAsync(ReadingHistory history, CancellationToken cancellationToken = default)
    {
        if (history == null)
            throw new ArgumentNullException(nameof(history));

        _context.Set<ReadingHistory>().Update(history);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var history = await GetByIdAsync(id, cancellationToken);
        if (history != null)
        {
            _context.Set<ReadingHistory>().Remove(history);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsForBookAsync(BookId bookId, CancellationToken cancellationToken = default)
    {
        if (bookId == null)
            throw new ArgumentNullException(nameof(bookId));

        return await _context.Set<ReadingHistory>()
            .AnyAsync(rh => rh.BookId == bookId, cancellationToken);
    }
}