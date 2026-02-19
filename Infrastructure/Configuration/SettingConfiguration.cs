using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class SettingConfiguration : IEntityTypeConfiguration<Setting>
{
    public void Configure(EntityTypeBuilder<Setting> builder)
    {
        builder.ToTable("Settings");

        // Primary key
        builder.HasKey(s => s.Key);

        // Properties
        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Value)
            .IsRequired();

        builder.Property(s => s.LastUpdated)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(s => s.LastUpdated);
    }
}