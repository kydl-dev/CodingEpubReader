using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BookRepository(EpubReaderDbContext context) : IBookRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Book?> GetByIdAsync(BookId id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .OrderByDescending(b => b.LastOpenedDate ?? DateTime.MinValue)
            .ThenBy(b => b.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Book>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync(cancellationToken);

        var lowerQuery = query.ToLower();

        return await _context.Books
            .Where(b => b.Title.ToLower().Contains(lowerQuery) ||
                        b.Authors.Any(a => a.ToLower().Contains(lowerQuery)))
            .OrderBy(b => b.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task<Book> AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        if (book == null)
            throw new ArgumentNullException(nameof(book));

        await _context.Books.AddAsync(book, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return book;
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        if (book == null)
            throw new ArgumentNullException(nameof(book));

        _context.Books.Update(book);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BookId id, CancellationToken cancellationToken = default)
    {
        var book = await GetByIdAsync(id, cancellationToken);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(BookId id, CancellationToken cancellationToken = default)
    {
        return await _context.Books
            .AnyAsync(b => b.Id == id, cancellationToken);
    }
}