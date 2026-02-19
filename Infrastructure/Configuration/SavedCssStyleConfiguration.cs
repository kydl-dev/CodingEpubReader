using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration;

public class SavedCssStyleConfiguration : IEntityTypeConfiguration<SavedCssStyle>
{
    public void Configure(EntityTypeBuilder<SavedCssStyle> builder)
    {
        builder.ToTable("SavedCssStyles");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.IsDefault)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.LastModifiedAt)
            .IsRequired();

        // Configure CssStyle as owned type
        builder.OwnsOne(s => s.Style, styleBuilder =>
        {
            styleBuilder.Property(css => css.FontFamily)
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnName("FontFamily");

            styleBuilder.Property(css => css.FontSize)
                .IsRequired()
                .HasColumnName("FontSize");

            styleBuilder.Property(css => css.LineHeight)
                .IsRequired()
                .HasColumnName("LineHeight");

            styleBuilder.Property(css => css.LetterSpacing)
                .IsRequired()
                .HasColumnName("LetterSpacing");

            styleBuilder.Property(css => css.MarginHorizontal)
                .IsRequired()
                .HasColumnName("MarginHorizontal");

            styleBuilder.Property(css => css.MarginVertical)
                .IsRequired()
                .HasColumnName("MarginVertical");

            styleBuilder.Property(css => css.CustomCss)
                .HasMaxLength(2000)
                .HasColumnName("CustomCss");

            // Configure ColorScheme as nested owned type
            styleBuilder.OwnsOne(css => css.Colors, colorBuilder =>
            {
                colorBuilder.Property(c => c.Background)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("ColorBackground");

                colorBuilder.Property(c => c.Text)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("ColorText");

                colorBuilder.Property(c => c.Link)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("ColorLink");

                colorBuilder.Property(c => c.Selection)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("ColorSelection");

                colorBuilder.Property(c => c.Surface)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("ColorSurface");

                colorBuilder.Property(c => c.Border)
                    .IsRequired()
                    .HasMaxLength(20)
                    .HasColumnName("ColorBorder");
            });
        });

        // Create unique index on Name to prevent duplicate style names
        builder.HasIndex(s => s.Name)
            .IsUnique();

        // Create index on IsDefault for quickly finding the default style
        builder.HasIndex(s => s.IsDefault);
    }
}