using System.Reflection;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class EpubReaderDbContext(DbContextOptions<EpubReaderDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Highlight> Highlights => Set<Highlight>();
    public DbSet<ReadingPosition> ReadingPositions => Set<ReadingPosition>();
    public DbSet<ReadingHistory> ReadingHistories => Set<ReadingHistory>();
    public DbSet<SavedCssStyle> SavedCssStyles => Set<SavedCssStyle>();
    public DbSet<Library> Libraries => Set<Library>();
    public DbSet<LibraryIndex> LibraryIndexes => Set<LibraryIndex>();
    public DbSet<Setting> Settings => Set<Setting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps or perform any other cross-cutting concerns here
        return await base.SaveChangesAsync(cancellationToken);
    }
}