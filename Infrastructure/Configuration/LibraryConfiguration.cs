using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class LibraryConfiguration : IEntityTypeConfiguration<Library>
{
    public void Configure(EntityTypeBuilder<Library> builder)
    {
        builder.ToTable("Libraries");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.Description)
            .HasMaxLength(1000);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.Property(l => l.LastUpdatedAt)
            .IsRequired();

        builder.Property(l => l.TotalBooks)
            .IsRequired();

        builder.Property(l => l.StoragePath)
            .HasMaxLength(500);

        // Create unique index on Name
        builder.HasIndex(l => l.Name)
            .IsUnique();
    }
}