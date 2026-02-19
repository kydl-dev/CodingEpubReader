using Domain.Entities;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class HighlightConfiguration : IEntityTypeConfiguration<Highlight>
{
    public void Configure(EntityTypeBuilder<Highlight> builder)
    {
        builder.ToTable("Highlights");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(h => h.BookId)
            .HasConversion(
                v => v.Value,
                v => BookId.From(v))
            .IsRequired();

        // Complex type mapping for TextRange
        builder.OwnsOne(h => h.TextRange, textRange =>
        {
            textRange.Property(tr => tr.ChapterId)
                .HasColumnName("ChapterId")
                .HasMaxLength(200)
                .IsRequired();

            textRange.Property(tr => tr.StartOffset)
                .HasColumnName("StartOffset")
                .IsRequired();

            textRange.Property(tr => tr.EndOffset)
                .HasColumnName("EndOffset")
                .IsRequired();

            textRange.Property(tr => tr.SelectedText)
                .HasColumnName("SelectedText")
                .HasMaxLength(5000)
                .IsRequired();

            // Create index on ChapterId using the owned entity builder
            textRange.HasIndex(tr => tr.ChapterId)
                .HasDatabaseName("IX_Highlights_ChapterId"); //
        });

        builder.Property(h => h.Color)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(h => h.Note)
            .HasMaxLength(2000);

        builder.Property(h => h.CreatedAt)
            .IsRequired();

        builder.Property(h => h.UpdatedAt);

        // Indexes
        builder.HasIndex(h => h.BookId);
        builder.HasIndex(h => h.CreatedAt);
        builder.HasIndex(h => new { h.BookId, h.Color });
    }
}