using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ReadingProgressRepository(EpubReaderDbContext context) : IReadingProgressRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<ReadingPosition?> GetLastPositionAsync(BookId bookId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReadingPositions
            .FirstOrDefaultAsync(rp => rp.BookId == bookId, cancellationToken);
    }

    public async Task SavePositionAsync(ReadingPosition position, CancellationToken cancellationToken = default)
    {
        if (position == null)
            throw new ArgumentNullException(nameof(position));

        var existingPosition = await GetLastPositionAsync(position.BookId, cancellationToken);

        if (existingPosition != null) _context.ReadingPositions.Remove(existingPosition);

        await _context.ReadingPositions.AddAsync(position, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(BookId bookId, CancellationToken cancellationToken = default)
    {
        var position = await GetLastPositionAsync(bookId, cancellationToken);
        if (position != null)
        {
            _context.ReadingPositions.Remove(position);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}