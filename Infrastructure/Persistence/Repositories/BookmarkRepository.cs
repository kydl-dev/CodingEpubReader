using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class BookmarkRepository(EpubReaderDbContext context) : IBookmarkRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Bookmark?> GetByIdAsync(Guid bookmarkId, CancellationToken cancellationToken = default)
    {
        var bookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.Id == bookmarkId, cancellationToken);

        if (bookmark != null) ReconstructPosition(bookmark);

        return bookmark;
    }

    public async Task<IEnumerable<Bookmark>> GetByBookIdAsync(BookId bookId,
        CancellationToken cancellationToken = default)
    {
        var bookmarks = await _context.Bookmarks
            .Where(b => b.BookId == bookId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var bookmark in bookmarks) ReconstructPosition(bookmark);

        return bookmarks;
    }

    public async Task<Bookmark> AddAsync(Bookmark bookmark, CancellationToken cancellationToken = default)
    {
        if (bookmark == null)
            throw new ArgumentNullException(nameof(bookmark));

        var entry = await _context.Bookmarks.AddAsync(bookmark, cancellationToken);
        SetPositionShadowProperties(entry.Entity, bookmark.Position);
        await _context.SaveChangesAsync(cancellationToken);

        return bookmark;
    }

    public async Task UpdateAsync(Bookmark bookmark, CancellationToken cancellationToken = default)
    {
        if (bookmark == null)
            throw new ArgumentNullException(nameof(bookmark));

        var entry = _context.Bookmarks.Update(bookmark);

        // Update Position properties as shadow properties
        SetPositionShadowProperties(entry.Entity, bookmark.Position);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid bookmarkId, CancellationToken cancellationToken = default)
    {
        var bookmark = await GetByIdAsync(bookmarkId, cancellationToken);
        if (bookmark != null)
        {
            _context.Bookmarks.Remove(bookmark);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // Reconstruct ReadingPosition from shadow properties when loading
    private void ReconstructPosition(Bookmark bookmark)
    {
        var entry = _context.Entry(bookmark);

        var positionBookId = (Guid)entry.Property("PositionBookId").CurrentValue!;
        var chapterId = (string)entry.Property("ChapterId").CurrentValue!;
        var progress = (double)entry.Property("Progress").CurrentValue!;
        var savedAt = (DateTime)entry.Property("PositionSavedAt").CurrentValue!;

        var position = new ReadingPosition(
            BookId.From(positionBookId),
            chapterId,
            progress);

        // Use reflection to set SavedAt (has private setter)
        var savedAtProperty = typeof(ReadingPosition).GetProperty("SavedAt");
        savedAtProperty?.SetValue(position, savedAt);

        // Use reflection to set Position on Bookmark (has private setter)
        var positionProperty = typeof(Bookmark).GetProperty("Position");
        positionProperty?.SetValue(bookmark, position);
    }

// Store Position properties as shadow properties when saving
    private void SetPositionShadowProperties(Bookmark bookmark, ReadingPosition position)
    {
        var entry = _context.Entry(bookmark);

        entry.Property("PositionBookId").CurrentValue = position.BookId.Value;
        entry.Property("ChapterId").CurrentValue = position.ChapterId;
        entry.Property("Progress").CurrentValue = position.Progress;
        entry.Property("PositionSavedAt").CurrentValue = position.SavedAt;
    }
}