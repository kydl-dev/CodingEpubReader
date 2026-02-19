using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Navigation.GetTableOfContents;

public class GetTableOfContentsQueryHandler(
    IBookRepository bookRepository,
    IMapper mapper,
    ILogger<GetTableOfContentsQueryHandler> logger,
    IEpubParser epubParser) : IRequestHandler<GetTableOfContentsQuery, IEnumerable<TocItemDto>>
{
    public async Task<IEnumerable<TocItemDto>> Handle(
        GetTableOfContentsQuery request, CancellationToken cancellationToken)
    {
        var bookId = BookId.From(request.BookId);
        var book = await bookRepository.GetByIdAsync(bookId, cancellationToken)
                   ?? throw new BookNotFoundException(request.BookId);

        var flatToc = book.TableOfContents
            .SelectMany(item => item.Flatten())
            .ToList();
        var tocWithContent = flatToc
            .Where(item => !string.IsNullOrWhiteSpace(item.ContentSrc))
            .ToList();
        var hasNestedToc = flatToc.Any(item => item.Depth > 0);
        var hasAnchoredTocLinks = tocWithContent.Any(item => item.ContentSrc.Contains('#'));
        var likelyMissingAnchors = hasNestedToc &&
                                   !hasAnchoredTocLinks &&
                                   tocWithContent.Count > book.Chapters.Count + 10;

        // Heal stale records: re-parse whenever TOC items have missing ContentSrc OR
        // the book has no chapters (both indicate a pre-fix import that stored incomplete data).
        var needsHeal = book.TableOfContents.Any(t => string.IsNullOrEmpty(t.ContentSrc))
                        || !book.Chapters.Any()
                        || likelyMissingAnchors;

        if (needsHeal)
        {
            logger.LogWarning(
                "Book {BookId} has stale data. Re-parsing from {FilePath}. MissingContent={MissingContent}, NoChapters={NoChapters}, MissingAnchors={MissingAnchors}",
                book.Id,
                book.FilePath,
                book.TableOfContents.Any(t => string.IsNullOrEmpty(t.ContentSrc)),
                !book.Chapters.Any(),
                likelyMissingAnchors);

            var reparsed = await epubParser.ParseAsync(book.FilePath, cancellationToken);

            // FIX: Heal BOTH TOC and chapters. The original import may have stored 0 chapters
            // (due to the path-matching bug), so we must update both or chapter lookups will
            // fail even after TOC is healed.
            book.UpdateTableOfContents(reparsed.TableOfContents);
            book.UpdateChapters(reparsed.Chapters);

            await bookRepository.UpdateAsync(book, cancellationToken);

            logger.LogInformation(
                "Healed book {BookId}: {TocCount} TOC items, {ChapterCount} chapters",
                book.Id, reparsed.TableOfContents.Count, reparsed.Chapters.Count);
        }

        return mapper.Map<IEnumerable<TocItemDto>>(book.TableOfContents);
    }
}