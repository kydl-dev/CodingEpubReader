using Application.Interfaces;
using Domain.Enums;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Books.GetChapterContent;

/// <summary>
///     Handles retrieval of processed chapter content with proper CSS styling.
///     Integrates SavedCssStyle preferences with IBookContentService rendering.
/// </summary>
public class GetChapterContentQueryHandler(
    IBookContentService contentService,
    ISavedCssStyleRepository styleRepository,
    IThemeService themeService,
    ILogger<GetChapterContentQueryHandler> logger)
    : IRequestHandler<GetChapterContentQuery, string>
{
    private readonly IBookContentService _contentService = contentService
                                                           ?? throw new ArgumentNullException(nameof(contentService));

    private readonly ILogger<GetChapterContentQueryHandler> _logger = logger
                                                                      ?? throw new ArgumentNullException(
                                                                          nameof(logger));

    private readonly ISavedCssStyleRepository _styleRepository = styleRepository
                                                                 ?? throw new ArgumentNullException(
                                                                     nameof(styleRepository));

    private readonly IThemeService _themeService = themeService
                                                   ?? throw new ArgumentNullException(nameof(themeService));

    public async Task<string> Handle(
        GetChapterContentQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting chapter content for book {BookId}, chapter {ChapterId}",
            request.BookId,
            request.ChapterId);

        CssStyle? style = null;

        try
        {
            // Try to load custom style if specified
            if (request.CustomStyleId.HasValue)
            {
                var savedStyle = await _styleRepository.GetByIdAsync(
                    request.CustomStyleId.Value,
                    cancellationToken);

                if (savedStyle != null)
                {
                    style = savedStyle.Style;
                    _logger.LogInformation(
                        "Using custom style: {StyleName}",
                        savedStyle.Name);
                }
                else
                {
                    _logger.LogWarning(
                        "Custom style {StyleId} not found, using default",
                        request.CustomStyleId.Value);
                }
            }

            // Fall back to user's default style
            if (style == null)
            {
                var defaultStyle = await _styleRepository.GetDefaultAsync(cancellationToken);

                if (defaultStyle != null)
                {
                    style = defaultStyle.Style;
                    _logger.LogInformation(
                        "Using default style: {StyleName}",
                        defaultStyle.Name);
                }
                else
                {
                    style = _themeService.CurrentTheme.Kind switch
                    {
                        ThemeKind.Dark => CssStyle.Dracula,
                        ThemeKind.Sepia => CssStyle.Sepia,
                        _ => CssStyle.Default
                    };

                    _logger.LogInformation(
                        "No saved default style found. Using theme-aware fallback style for UI theme {ThemeKind}.",
                        _themeService.CurrentTheme.Kind);
                }
            }

            // Generate and return the styled HTML content
            var content = await _contentService.GetChapterContentAsync(
                request.BookId,
                request.ChapterId,
                style,
                cancellationToken);

            _logger.LogInformation(
                "Successfully generated chapter content ({Length} chars)",
                content.Length);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting chapter content for book {BookId}, chapter {ChapterId}",
                request.BookId,
                request.ChapterId);
            throw;
        }
    }
}