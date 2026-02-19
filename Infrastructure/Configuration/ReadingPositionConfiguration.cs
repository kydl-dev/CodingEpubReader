using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class ReadingPositionConfiguration : IEntityTypeConfiguration<ReadingPosition>
{
    public void Configure(EntityTypeBuilder<ReadingPosition> builder)
    {
        builder.ToTable("ReadingPositions");

        // Composite key: BookId
        builder.HasKey(rp => rp.BookId);

        builder.Property(rp => rp.BookId)
            .HasConversion(
                v => v.Value,
                v => BookId.From(v))
            .IsRequired();

        builder.Property(rp => rp.ChapterId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(rp => rp.Progress)
            .IsRequired();

        builder.Property(rp => rp.SavedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(rp => rp.SavedAt);
    }
}