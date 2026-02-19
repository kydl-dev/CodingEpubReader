using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class LibraryRepository(EpubReaderDbContext context) : ILibraryRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Library?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Library>()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Library?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return await _context.Set<Library>()
            .FirstOrDefaultAsync(l => l.Name == name, cancellationToken);
    }

    public async Task<Library?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        // Return the first library as default (you can add a IsDefault property if needed)
        return await _context.Set<Library>()
            .OrderBy(l => l.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<Library>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<Library>()
            .OrderBy(l => l.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Library> AddAsync(Library library, CancellationToken cancellationToken = default)
    {
        if (library == null)
            throw new ArgumentNullException(nameof(library));

        await _context.Set<Library>().AddAsync(library, cancellationToken);
        return library;
    }

    public Task UpdateAsync(Library library, CancellationToken cancellationToken = default)
    {
        if (library == null)
            throw new ArgumentNullException(nameof(library));

        _context.Set<Library>().Update(library);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var library = await GetByIdAsync(id, cancellationToken);
        if (library != null) _context.Set<Library>().Remove(library);
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return await _context.Set<Library>()
            .AnyAsync(l => l.Name == name, cancellationToken);
    }
}