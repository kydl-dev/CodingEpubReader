using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class HighlightRepository(EpubReaderDbContext context) : IHighlightRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Highlight?> GetByIdAsync(Guid highlightId, CancellationToken cancellationToken = default)
    {
        return await _context.Highlights
            .FirstOrDefaultAsync(h => h.Id == highlightId, cancellationToken);
    }

    public async Task<IEnumerable<Highlight>> GetByBookIdAsync(BookId bookId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Highlights
            .Where(h => h.BookId == bookId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Highlight>> GetByChapterAsync(BookId bookId, string chapterId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Highlights
            .Where(h => h.BookId == bookId && h.TextRange.ChapterId == chapterId)
            .OrderBy(h => h.TextRange.StartOffset)
            .ToListAsync(cancellationToken);
    }

    public async Task<Highlight> AddAsync(Highlight highlight, CancellationToken cancellationToken = default)
    {
        if (highlight == null)
            throw new ArgumentNullException(nameof(highlight));

        await _context.Highlights.AddAsync(highlight, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return highlight;
    }

    public async Task UpdateAsync(Highlight highlight, CancellationToken cancellationToken = default)
    {
        if (highlight == null)
            throw new ArgumentNullException(nameof(highlight));

        _context.Highlights.Update(highlight);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid highlightId, CancellationToken cancellationToken = default)
    {
        var highlight = await GetByIdAsync(highlightId, cancellationToken);
        if (highlight != null)
        {
            _context.Highlights.Remove(highlight);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}