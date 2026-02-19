using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class LibraryIndexRepository(EpubReaderDbContext context) : ILibraryIndexRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<LibraryIndex?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<LibraryIndex>()
            .Include(li => li.Book)
            .FirstOrDefaultAsync(li => li.Id == id, cancellationToken);
    }

    public async Task<LibraryIndex?> GetByBookIdAsync(BookId bookId, CancellationToken cancellationToken = default)
    {
        if (bookId == null)
            throw new ArgumentNullException(nameof(bookId));

        return await _context.Set<LibraryIndex>()
            .Include(li => li.Book)
            .FirstOrDefaultAsync(li => li.BookId == bookId, cancellationToken);
    }

    public async Task<IEnumerable<LibraryIndex>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<LibraryIndex>()
            .Include(li => li.Book)
            .OrderBy(li => li.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LibraryIndex>> SearchAsync(string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var lowerQuery = query.ToLower();

        return await _context.Set<LibraryIndex>()
            .Include(li => li.Book)
            .Where(li => li.Title.ToLower().Contains(lowerQuery)
                         || li.Author.ToLower().Contains(lowerQuery)
                         || (li.Tags != null && li.Tags.ToLower().Contains(lowerQuery)))
            .OrderBy(li => li.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LibraryIndex>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<LibraryIndex>()
            .Include(li => li.Book)
            .Where(li => li.IsFavorite)
            .OrderBy(li => li.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LibraryIndex>> GetRecentlyAccessedAsync(int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<LibraryIndex>()
            .Include(li => li.Book)
            .Where(li => li.LastAccessedAt != null)
            .OrderByDescending(li => li.LastAccessedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<LibraryIndex>> GetByTagAsync(string tag,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return [];

        var lowerTag = tag.ToLower();

        return await _context.Set<LibraryIndex>()
            .Include(li => li.Book)
            .Where(li => li.Tags != null && li.Tags.ToLower().Contains(lowerTag))
            .OrderBy(li => li.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<LibraryIndex> AddAsync(LibraryIndex index, CancellationToken cancellationToken = default)
    {
        if (index == null)
            throw new ArgumentNullException(nameof(index));

        await _context.Set<LibraryIndex>().AddAsync(index, cancellationToken);
        return index;
    }

    public Task UpdateAsync(LibraryIndex index, CancellationToken cancellationToken = default)
    {
        if (index == null)
            throw new ArgumentNullException(nameof(index));

        _context.Set<LibraryIndex>().Update(index);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var index = await GetByIdAsync(id, cancellationToken);
        if (index != null) _context.Set<LibraryIndex>().Remove(index);
    }

    public async Task<bool> ExistsForBookAsync(BookId bookId, CancellationToken cancellationToken = default)
    {
        if (bookId == null)
            throw new ArgumentNullException(nameof(bookId));

        return await _context.Set<LibraryIndex>()
            .AnyAsync(li => li.BookId == bookId, cancellationToken);
    }
}