using System.Text.Json;
using Domain.Entities;
using Domain.ValueObjects;
using Infrastructure.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    /// <summary>
    ///     JSON options used for serialising / deserialising the TableOfContents column.
    ///     The custom TocItemConverter handles:
    ///     • The ReadOnlyCollection vs IEnumerable constructor-parameter type mismatch
    ///     that makes STJ's default parameterised-constructor path fail.
    ///     • Existing rows that were stored before ContentSrc / Children became public
    ///     (those fields are simply absent in old JSON — the converter defaults them
    ///     to empty string / empty list rather than crashing).
    /// </summary>
    private static readonly JsonSerializerOptions TocOptions = new()
    {
        Converters = { new TocItemConverter() }
    };

    // Value comparer for List<TocItem>: always treat the collection as modified
    // when SaveChanges is called on an entity whose _tableOfContents was mutated
    // via Clear()+AddRange(). Without this, EF Core uses reference equality and
    // misses in-place mutations since the list reference never changes.
    private static readonly ValueComparer<List<TocItem>> TocComparer = new(
        (a, b) => a != null && b != null && a.SequenceEqual(b),
        v => v.Aggregate(0, (h, t) => HashCode.Combine(h, t.GetHashCode())),
        v => v.ToList());

    // Same rationale for List<Book.ChapterData>.
    private static readonly ValueComparer<List<Book.ChapterData>> ChapterComparer = new(
        (a, b) => a != null && b != null && a.Count == b.Count &&
                  a.Zip(b).All(pair => pair.First.Id == pair.Second.Id),
        v => v.Aggregate(0, (h, c) => HashCode.Combine(h, c.Id.GetHashCode())),
        v => v.ToList());

    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey("Id");

        builder.Property(b => b.Id)
            .HasConversion(
                v => v.Value,
                v => BookId.From(v))
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(b => b.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.Authors)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!.AsReadOnly())
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Property(b => b.Language)
            .HasMaxLength(10)
            .IsRequired();

        builder.OwnsOne(b => b.Metadata, metadata =>
        {
            metadata.Property(m => m.Publisher).HasMaxLength(300);
            metadata.Property(m => m.Description).HasColumnType("TEXT");
            metadata.Property(m => m.Isbn).HasMaxLength(50);
            metadata.Property(m => m.GoogleBooksId).HasMaxLength(100);
            metadata.Property(m => m.CalibreId).HasMaxLength(100);
            metadata.Property(m => m.Uuid).HasMaxLength(100);
            metadata.Property(m => m.Subject).HasMaxLength(300);
            metadata.Property(m => m.Rights).HasColumnType("TEXT");
            metadata.Property(m => m.PublishedDate);
            metadata.Property(m => m.CoverImagePath).HasMaxLength(500);
            metadata.Property(m => m.EpubVersion).HasMaxLength(20);

            metadata.Property(m => m.Format)
                .HasConversion<string>()
                .HasMaxLength(50);

            metadata.Property(m => m.Creators)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!.AsReadOnly())
                .HasColumnType("TEXT");
        });

        // Store Chapters as JSON — with a value comparer so EF Core detects in-place mutations.
        builder.Property<List<Book.ChapterData>>("_chapters")
            .HasColumnName("Chapters")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Book.ChapterData>>(v, (JsonSerializerOptions?)null) ??
                     new List<Book.ChapterData>(),
                ChapterComparer)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Ignore(b => b.Chapters);

        // Store TableOfContents as JSON — with a value comparer and TocItemConverter for
        // backward compatibility with rows written before ContentSrc / Children were public.
        builder.Property<List<TocItem>>("_tableOfContents")
            .HasColumnName("TableOfContents")
            .HasConversion(
                v => JsonSerializer.Serialize(v, TocOptions),
                v => JsonSerializer.Deserialize<List<TocItem>>(v, TocOptions) ?? new List<TocItem>(),
                TocComparer)
            .HasColumnType("TEXT")
            .IsRequired();

        builder.Ignore(b => b.TableOfContents);

        builder.Property(b => b.FilePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(b => b.AddedDate)
            .IsRequired();

        builder.Property(b => b.LastOpenedDate);

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.AddedDate);
        builder.HasIndex(b => b.LastOpenedDate);
    }
}