using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ReadingHistoryConfiguration : IEntityTypeConfiguration<ReadingHistory>
{
    public void Configure(EntityTypeBuilder<ReadingHistory> builder)
    {
        builder.ToTable("ReadingHistories");

        builder.HasKey(rh => rh.Id);

        builder.Property(rh => rh.Id)
            .IsRequired()
            .ValueGeneratedNever();

        // BookId is nullable — it is set to NULL when the book is deleted so that
        // the history record survives. The metadata snapshot (BookTitle, BookAuthor,
        // BookIsbn) keeps it meaningful without the FK.
        builder.Property(rh => rh.BookId)
            .HasConversion(
                bookId => bookId != null ? bookId.Value : (Guid?)null,
                value => value.HasValue ? BookId.From(value.Value) : null)
            .IsRequired(false);

        // Book metadata snapshot — always populated at creation time.
        builder.Property(rh => rh.BookTitle)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rh => rh.BookAuthor)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rh => rh.BookIsbn)
            .IsRequired(false)
            .HasMaxLength(50);

        builder.Property(rh => rh.LastReadAt)
            .IsRequired();

        builder.Property(rh => rh.TotalReadingTime)
            .IsRequired();

        builder.Property(rh => rh.TotalSessions)
            .IsRequired();

        // SetNull: when the Book row is deleted, BookId becomes NULL here instead of
        // cascading the delete to the history record.
        builder.HasOne(rh => rh.Book)
            .WithMany()
            .HasForeignKey(rh => rh.BookId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on BookId for fast lookups (nullable, so not unique — a deleted book
        // leaves a NULL row and future books shouldn't collide).
        builder.HasIndex(rh => rh.BookId);

        // Index on LastReadAt for sorting recent reads.
        builder.HasIndex(rh => rh.LastReadAt);
    }
}