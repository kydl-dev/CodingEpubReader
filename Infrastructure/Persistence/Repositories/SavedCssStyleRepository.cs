using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class SavedCssStyleRepository(EpubReaderDbContext context) : ISavedCssStyleRepository
{
    private readonly EpubReaderDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<SavedCssStyle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<SavedCssStyle>()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<SavedCssStyle?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return await _context.Set<SavedCssStyle>()
            .FirstOrDefaultAsync(s => s.Name == name, cancellationToken);
    }

    public async Task<SavedCssStyle?> GetDefaultAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<SavedCssStyle>()
            .FirstOrDefaultAsync(s => s.IsDefault, cancellationToken);
    }

    public async Task<IEnumerable<SavedCssStyle>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<SavedCssStyle>()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<SavedCssStyle> AddAsync(SavedCssStyle style, CancellationToken cancellationToken = default)
    {
        if (style == null)
            throw new ArgumentNullException(nameof(style));

        EnsureStyleIntegrity(style);
        await _context.Set<SavedCssStyle>().AddAsync(style, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return style;
    }

    public async Task UpdateAsync(SavedCssStyle style, CancellationToken cancellationToken = default)
    {
        if (style == null)
            throw new ArgumentNullException(nameof(style));

        EnsureStyleIntegrity(style);
        var existing = await _context.Set<SavedCssStyle>()
            .FirstOrDefaultAsync(s => s.Id == style.Id, cancellationToken);

        if (existing == null)
            throw new InvalidOperationException($"Cannot update style {style.Id} because it does not exist.");

        existing.UpdateName(style.Name);
        existing.UpdateStyle(style.Style);
        if (style.IsDefault)
            existing.SetAsDefault();
        else
            existing.UnsetDefault();

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var style = await GetByIdAsync(id, cancellationToken);
        if (style != null)
        {
            _context.Set<SavedCssStyle>().Remove(style);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return await _context.Set<SavedCssStyle>()
            .AnyAsync(s => s.Name == name, cancellationToken);
    }

    public async Task ClearAllDefaultsAsync(CancellationToken cancellationToken = default)
    {
        await _context.Set<SavedCssStyle>()
            .Where(s => s.IsDefault)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.IsDefault, false)
                .SetProperty(s => s.LastModifiedAt, DateTime.UtcNow), cancellationToken);
    }

    private static void EnsureStyleIntegrity(SavedCssStyle style)
    {
        var fallback = GetFallbackStyleByName(style.Name);
        var source = style.Style ?? fallback;
        var sourceColors = source.Colors ?? fallback.Colors;

        var normalized = GetFallbackStyleByName(style.Name);
        normalized.FontFamily = string.IsNullOrWhiteSpace(source.FontFamily) ? fallback.FontFamily : source.FontFamily;
        normalized.FontSize = source.FontSize <= 0 ? fallback.FontSize : source.FontSize;
        normalized.LineHeight = source.LineHeight <= 0 ? fallback.LineHeight : source.LineHeight;
        normalized.LetterSpacing = source.LetterSpacing;
        normalized.MarginHorizontal =
            source.MarginHorizontal <= 0 ? fallback.MarginHorizontal : source.MarginHorizontal;
        normalized.MarginVertical = source.MarginVertical <= 0 ? fallback.MarginVertical : source.MarginVertical;
        normalized.CustomCss = source.CustomCss ?? fallback.CustomCss;
        normalized.Colors = new ColorScheme(
            ReadOrFallback(sourceColors.Background, fallback.Colors.Background),
            ReadOrFallback(sourceColors.Text, fallback.Colors.Text),
            ReadOrFallback(sourceColors.Link, fallback.Colors.Link),
            ReadOrFallback(sourceColors.Selection, fallback.Colors.Selection),
            ReadOrFallback(sourceColors.Surface, fallback.Colors.Surface),
            ReadOrFallback(sourceColors.Border, fallback.Colors.Border));

        style.UpdateStyle(normalized);
    }

    private static CssStyle GetFallbackStyleByName(string? name)
    {
        if (string.Equals(name, "Sepia", StringComparison.OrdinalIgnoreCase)) return CssStyle.Sepia;

        if (string.Equals(name, "Dracula", StringComparison.OrdinalIgnoreCase)) return CssStyle.Dracula;

        return CssStyle.Default;
    }

    private static string ReadOrFallback(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}