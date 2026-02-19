using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class BookmarkConfiguration : IEntityTypeConfiguration<Bookmark>
{
    public void Configure(EntityTypeBuilder<Bookmark> builder)
    {
        builder.ToTable("Bookmarks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(b => b.BookId)
            .HasConversion(
                v => v.Value,
                v => BookId.From(v))
            .IsRequired();

        builder.Ignore(b => b.Position);

        // Map Position properties as columns
        builder.Property<Guid>("PositionBookId")
            .HasColumnName("PositionBookId")
            .IsRequired();

        builder.Property<string>("ChapterId")
            .HasColumnName("ChapterId")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property<double>("Progress")
            .HasColumnName("Progress")
            .IsRequired();

        builder.Property<DateTime>("PositionSavedAt")
            .HasColumnName("PositionSavedAt")
            .IsRequired();

        builder.Property(b => b.Note)
            .HasMaxLength(2000);

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Indexes
        builder.HasIndex(b => b.BookId);
        builder.HasIndex(b => b.CreatedAt);
        builder.HasIndex(b => new { b.BookId, b.Type });
    }
}