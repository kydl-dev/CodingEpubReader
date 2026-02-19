using System.Text.RegularExpressions;
using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces;
using AutoMapper;
using Domain.Repositories;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class SearchService(
    IBookRepository bookRepository,
    IMapper mapper,
    ILogger<SearchService> logger)
    : ISearchService
{
    private readonly IBookRepository _bookRepository =
        bookRepository ?? throw new ArgumentNullException(nameof(bookRepository));

    private readonly Domain.Services.SearchService _domainSearchService = new();
    private readonly ILogger<SearchService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<IEnumerable<SearchResultDto>> SearchInBookAsync(
        Guid bookId,
        string query,
        bool caseSensitive = false,
        bool wholeWord = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Empty search query provided");
            return [];
        }

        _logger.LogInformation("Searching in book {BookId} for query: {Query}", bookId, query);

        var bookIdValue = BookId.From(bookId);
        var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);

        if (book == null)
        {
            _logger.LogWarning("Book not found: {BookId}", bookId);
            throw new BookNotFoundException(bookId);
        }

        // Use domain search service to perform the search
        var searchResults = _domainSearchService.SearchInBook(book, query, caseSensitive, wholeWord);

        var resultDtos = _mapper.Map<IEnumerable<SearchResultDto>>(searchResults);

        var searchInBookAsync = resultDtos.ToList();
        _logger.LogInformation("Found {ResultCount} search results in book {BookId}",
            searchInBookAsync.Count, bookId);

        return searchInBookAsync;
    }

    public async Task<IEnumerable<string>> GetSuggestionsAsync(
        Guid bookId,
        string partialQuery,
        int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(partialQuery) || partialQuery.Length < 2) return [];

        _logger.LogDebug("Getting search suggestions for book {BookId}, query: {Query}",
            bookId, partialQuery);

        var bookIdValue = BookId.From(bookId);
        var book = await _bookRepository.GetByIdAsync(bookIdValue, cancellationToken);

        if (book == null)
        {
            _logger.LogWarning("Book not found: {BookId}", bookId);
            return [];
        }

        // Extract unique words from chapters
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lowerPartialQuery = partialQuery.ToLowerInvariant();

        foreach (var chapter in book.Chapters)
        {
            // Simple word extraction (can be enhanced with better tokenization)
            var chapterWords = Regex
                .Split(chapter.HtmlContent, @"\W+")
                .Where(w => !string.IsNullOrWhiteSpace(w) && w.Length >= 3)
                .Where(w => w.ToLowerInvariant().StartsWith(lowerPartialQuery));

            foreach (var word in chapterWords)
            {
                words.Add(word);
                if (words.Count >= maxResults * 2) // Collect more than needed
                    break;
            }

            if (words.Count >= maxResults * 2)
                break;
        }

        var suggestions = words
            .OrderBy(w => w.Length)
            .ThenBy(w => w)
            .Take(maxResults)
            .ToList();

        _logger.LogDebug("Returning {SuggestionCount} suggestions", suggestions.Count);

        return suggestions;
    }
}