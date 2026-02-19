using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
///     Application-level search service. Delegates core search logic to the
///     <c>Domain.Services.SearchService</c> and maps results to DTOs.
/// </summary>
public interface ISearchService
{
    /// <summary>Searches all chapters of a book for the given query.</summary>
    Task<IEnumerable<SearchResultDto>> SearchInBookAsync(
        Guid bookId,
        string query,
        bool caseSensitive = false,
        bool wholeWord = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Returns autocomplete suggestions based on words already indexed for the book.
    /// </summary>
    Task<IEnumerable<string>> GetSuggestionsAsync(
        Guid bookId,
        string partialQuery,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
}