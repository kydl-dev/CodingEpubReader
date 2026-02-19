using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class LibraryIndexConfiguration : IEntityTypeConfiguration<LibraryIndex>
{
    public void Configure(EntityTypeBuilder<LibraryIndex> builder)
    {
        builder.ToTable("LibraryIndexes");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.Id)
            .IsRequired()
            .ValueGeneratedNever();

        // Configure BookId as owned type
        builder.Property(li => li.BookId)
            .HasConversion(
                bookId => bookId.Value,
                value => BookId.From(value))
            .IsRequired();

        builder.Property(li => li.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(li => li.Author)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(li => li.IndexedAt)
            .IsRequired();

        builder.Property(li => li.LastAccessedAt);

        builder.Property(li => li.AccessCount)
            .IsRequired();

        builder.Property(li => li.Tags)
            .HasMaxLength(500);

        builder.Property(li => li.IsFavorite)
            .IsRequired();

        // Create relationship with Book
        builder.HasOne(li => li.Book)
            .WithMany() // Book doesn't have a navigation property to LibraryIndex
            .HasForeignKey(li => li.BookId)
            .OnDelete(DeleteBehavior.Cascade);

        // Create unique index on BookId - each book should have only one index entry
        builder.HasIndex(li => li.BookId)
            .IsUnique();

        // Create index on Title for search performance
        builder.HasIndex(li => li.Title);

        // Create index on Author for search performance
        builder.HasIndex(li => li.Author);

        // Create index on IsFavorite for filtering favorites
        builder.HasIndex(li => li.IsFavorite);

        // Create index on LastAccessedAt for sorting by recent access
        builder.HasIndex(li => li.LastAccessedAt);
    }
}